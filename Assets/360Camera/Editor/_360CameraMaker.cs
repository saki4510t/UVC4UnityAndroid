// Copyright (c) 2015 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class _360CameraMaker
{
	public enum Side
	{
		Front,
		Back,
		Left,
		Right,
		Up,
		Down,
		Max
	}

	public const string RendererLayerName = "UI";

	public const string BaseFolderAssetPath = "Assets/360Camera";
	public const string RenderTexturesFolderAssetPath = "Assets/360Camera/RenderTextures";

	public const string CapturePrefabName = "360Camera";
	public const string CapturePrefabAssetPath = "Assets/360Camera/360Camera.prefab";

	public const string RendererPrefabName = "360CameraRealtimeEquirectanguler";
	public const string RendererShaderName = "360Camera/RealtimeEquirectangular";
	public const string RendererPrefabAssetPath = "Assets/360Camera/360CameraRealtimeEquirectanguler.prefab";
	public const string RendererMaterialAssetPath = "Assets/360Camera/360CameraRealtimeEquirectanguler.mat";

	public static string GetRenderTexturePath( Side side )
	{
		return RenderTexturesFolderAssetPath + "/" + side.ToString() + ".renderTexture";
	}

	[MenuItem( "Assets/Create/360 Camera/360 Camera" )]
	public static void Init()
	{
		InitFolder();
		InitRenderTextures();

		if( !File.Exists( CapturePrefabAssetPath ) ) {
			Quaternion[] rotations = new Quaternion[6] {
				Quaternion.Euler( 0.0f, 0.0f, 0.0f ),
				Quaternion.Euler( 0.0f, 180.0f, 0.0f ),
				Quaternion.Euler( 0.0f, 270.0f, 0.0f ),
				Quaternion.Euler( 0.0f, 90.0f, 0.0f ),
				Quaternion.Euler( 270.0f, 0.0f, 0.0f ),
				Quaternion.Euler( 90.0f, 0.0f, 0.0f )
			};

			GameObject parent = EditorUtility.CreateGameObjectWithHideFlags( CapturePrefabName, HideFlags.HideInHierarchy );
			for( int i = 0; i < (int)Side.Max; ++i ) {
				GameObject go = EditorUtility.CreateGameObjectWithHideFlags( ((Side)i).ToString(), HideFlags.HideInHierarchy );
				go.transform.localRotation = rotations[i];
				go.transform.SetParent( parent.transform, true );
				Camera camera = go.AddComponent<Camera>();
				int layerUI = LayerMask.NameToLayer( RendererLayerName );
				if( layerUI >= 0 ) {
					camera.cullingMask &= ~(1 << layerUI); // Exclude UI.
				}
				camera.fieldOfView = 90.0f;
				FixCameraProjectionMatrix( camera );
			}

			PrefabUtility.CreatePrefab( CapturePrefabAssetPath, parent );

			Editor.DestroyImmediate( parent );

			parent = AssetDatabase.LoadAssetAtPath<GameObject>( CapturePrefabAssetPath );

			foreach( Transform child in parent.transform ) {
				string renderTexturePath = RenderTexturesFolderAssetPath + "/" + child.name + ".renderTexture";
				RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>( renderTexturePath );
				Camera camera = child.gameObject.GetComponent<Camera>();
				camera.targetTexture = renderTexture;
			}

			EditorUtility.SetDirty( parent );
			AssetDatabase.SaveAssets();
		}
	}

	[MenuItem( "Assets/Create/360 Camera/360 Camera Realtime Equirectanguler" )]
	public static void InitRealtimeEquirectanguler()
	{
		InitFolder();
		InitRenderTextures();
		InitMaterial();

		if( !File.Exists( RendererPrefabAssetPath ) ) {
			GameObject go = EditorUtility.CreateGameObjectWithHideFlags( RendererPrefabName, HideFlags.HideInHierarchy );

			PrefabUtility.CreatePrefab( RendererPrefabAssetPath, go );
			Editor.DestroyImmediate( go );

			go = AssetDatabase.LoadAssetAtPath< GameObject >( RendererPrefabAssetPath );

			go.transform.localRotation = Quaternion.Euler( 270.0f, 0.0f, 0.0f );
			go.transform.localScale = new Vector3( 2.0f, 1.0f, 1.0f );

			MeshFilter meshFilter = go.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

			meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>( "New-Plane.fbx" );
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			meshRenderer.useLightProbes = false;
			meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

			meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>( RendererMaterialAssetPath );

			int layerUI = LayerMask.NameToLayer( RendererLayerName );
			if( layerUI >= 0 ) {
				go.layer = layerUI; // Only UI.
			}

			EditorUtility.SetDirty( go );
			AssetDatabase.SaveAssets();
		}
	}

	public static void InitFolder()
	{
		if( !Directory.Exists( BaseFolderAssetPath ) ) {
			Directory.CreateDirectory( BaseFolderAssetPath );
			AssetDatabase.ImportAsset( BaseFolderAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate );
		}
	}

	public static void InitRenderTextures()
	{
		if( !Directory.Exists( RenderTexturesFolderAssetPath ) ) {
			Directory.CreateDirectory( RenderTexturesFolderAssetPath );
			AssetDatabase.ImportAsset( RenderTexturesFolderAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate );
		}

		{
			for( int i = 0; i < (int)Side.Max; ++i ) {
				string renderTexturePath = GetRenderTexturePath( (Side)i );
				if( !File.Exists( renderTexturePath ) ) {
					RenderTexture renderTexture = new RenderTexture( 1024, 1024, 24 );
					AssetDatabase.CreateAsset( renderTexture, renderTexturePath );
					AssetDatabase.ImportAsset( renderTexturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate );
				}
			}
		}
	}

	public static void InitMaterial()
	{
		if( !File.Exists( RendererMaterialAssetPath ) ) {
			Material material = new Material( Shader.Find( RendererShaderName ) );

			for( int i = 0; i < (int)Side.Max; ++i ) {
				material.SetTexture( "_" + ((Side)i).ToString() + "Tex", AssetDatabase.LoadAssetAtPath<RenderTexture>( GetRenderTexturePath( (Side)i ) ) );
			}

			AssetDatabase.CreateAsset( material, RendererMaterialAssetPath );
		}
	}

	public static void FixCameraProjectionMatrix( Camera camera )
	{
		float left = -camera.nearClipPlane;
		float right = camera.nearClipPlane;
		float bottom = -camera.nearClipPlane;
		float top = camera.nearClipPlane;
		float near = camera.nearClipPlane;
		float far = camera.farClipPlane;
		float x = 2.0f * near / (right - left);
		float y = 2.0f * near / (top - bottom);
		float a = (right + left) / (right - left);
		float b = (top + bottom) / (top - bottom);
		float c = -(far + near) / (far - near);
		float d = -(2.0f * far * near) / (far - near);
		float e = -1.0f;
		Matrix4x4 m = new Matrix4x4();
		m[0, 0] = x;
		m[0, 1] = 0.0f;
		m[0, 2] = a;
		m[0, 3] = 0.0f;
		m[1, 0] = 0.0f;
		m[1, 1] = y;
		m[1, 2] = b;
		m[1, 3] = 0.0f;
		m[2, 0] = 0.0f;
		m[2, 1] = 0.0f;
		m[2, 2] = c;
		m[2, 3] = d;
		m[3, 0] = 0.0f;
		m[3, 1] = 0.0f;
		m[3, 2] = e;
		m[3, 3] = 0.0f;
		camera.projectionMatrix = m;
	}
}

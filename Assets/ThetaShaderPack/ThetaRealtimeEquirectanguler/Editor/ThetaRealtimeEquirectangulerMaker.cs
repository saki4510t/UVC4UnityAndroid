// Copyright (c) 2015 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
using UnityEngine;
using UnityEditor;
using System.IO;

public class ThetaRealtimeEquirectangulerMaker
{
	public const string RendererLayerName = "UI";

	public const string RendererShaderName = "Theta/RealtimeEquirectangular";

	public const string BaseFolderAssetPath = "Assets/ThetaRealtimeEquirectanguler";

	public const string MaterialName_Both = "ThetaRealtimeEquirectanguler (Both)";
	public const string MaterialName_Forward = "ThetaRealtimeEquirectanguler (Forward)";
	public const string MaterialName_Backward = "ThetaRealtimeEquirectanguler (Backward)";

	public const string PrefabName_Both = "ThetaRealtimeEquirectanguler (Both)";
	public const string PrefabName_ForwardBackward = "ThetaRealtimeEquirectanguler (Forward and Backward)";

	public const string MaterialAssetPath_Both = BaseFolderAssetPath + "/" + MaterialName_Both + ".mat";
	public const string MaterialAssetPath_Forward = BaseFolderAssetPath + "/" + MaterialName_Forward + ".mat";
	public const string MaterialAssetPath_Backward = BaseFolderAssetPath + "/" + MaterialName_Backward + ".mat";

	public const string PrefabAssetPath_Both = BaseFolderAssetPath + "/" + PrefabName_Both + ".prefab";
	public const string PrefabAssetPath_ForwardBackward = BaseFolderAssetPath + "/" + PrefabName_ForwardBackward + ".prefab";

	[MenuItem( "Assets/Create/Tehta/RealtimeEquirectanguler (Both)" )]
	public static void InitBoth()
	{
		InitFolder();
		InitMaterial_Both();

		if( !File.Exists( PrefabAssetPath_Both ) ) {
			GameObject go = EditorUtility.CreateGameObjectWithHideFlags( PrefabName_Both, HideFlags.HideInHierarchy );

			PrefabUtility.CreatePrefab( PrefabAssetPath_Both, go );
			Editor.DestroyImmediate( go );

			go = AssetDatabase.LoadAssetAtPath<GameObject>( PrefabAssetPath_Both );

			go.transform.localRotation = Quaternion.Euler( 270.0f, 0.0f, 0.0f );
			go.transform.localScale = new Vector3( 2.0f, 1.0f, 1.0f );

			MeshFilter meshFilter = go.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

			meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>( "New-Plane.fbx" );
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			meshRenderer.useLightProbes = false;
			meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

			meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>( MaterialAssetPath_Both );

			int layerUI = LayerMask.NameToLayer( RendererLayerName );
			if( layerUI >= 0 ) {
				go.layer = layerUI; // Only UI.
			}

			EditorUtility.SetDirty( go );
			AssetDatabase.SaveAssets();
		}
	}

	[MenuItem( "Assets/Create/Tehta/RealtimeEquirectanguler (Forward and Backward)" )]
	public static void InitForwardBackward()
	{
		InitFolder();
		InitMaterial_ForwardBackward();

		if( !File.Exists( PrefabAssetPath_ForwardBackward ) ) {
			GameObject go = EditorUtility.CreateGameObjectWithHideFlags( PrefabAssetPath_ForwardBackward, HideFlags.HideInHierarchy );

			int layerUI = LayerMask.NameToLayer( RendererLayerName );
			if( layerUI >= 0 ) {
				go.layer = layerUI; // Only UI.
			}

			{
				GameObject forward = EditorUtility.CreateGameObjectWithHideFlags( "Forward", HideFlags.HideInHierarchy );
				forward.transform.parent = go.transform;
				if( layerUI >= 0 ) {
					forward.layer = layerUI; // Only UI.
				}
				forward.transform.localPosition = new Vector3( -5.0f, 0.0f, 0.0f );
				forward.transform.localRotation = Quaternion.Euler( 270.0f, 0.0f, 0.0f );
				MeshFilter meshFilter = forward.AddComponent<MeshFilter>();
				MeshRenderer meshRenderer = forward.AddComponent<MeshRenderer>();

				meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>( "New-Plane.fbx" );
				meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				meshRenderer.receiveShadows = false;
				meshRenderer.useLightProbes = false;
				meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

				meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>( MaterialAssetPath_Forward );
			}
			{
				GameObject backward = EditorUtility.CreateGameObjectWithHideFlags( "Backward", HideFlags.HideInHierarchy );
				backward.transform.parent = go.transform;
				if( layerUI >= 0 ) {
					backward.layer = layerUI; // Only UI.
				}
				backward.transform.localPosition = new Vector3( 5.0f, 0.0f, 0.0f );
				backward.transform.localRotation = Quaternion.Euler( 270.0f, 0.0f, 0.0f );
				MeshFilter meshFilter = backward.AddComponent<MeshFilter>();
				MeshRenderer meshRenderer = backward.AddComponent<MeshRenderer>();

				meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>( "New-Plane.fbx" );
				meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				meshRenderer.receiveShadows = false;
				meshRenderer.useLightProbes = false;
				meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

				meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>( MaterialAssetPath_Backward );
			}

			PrefabUtility.CreatePrefab( PrefabAssetPath_ForwardBackward, go );
			Editor.DestroyImmediate( go );
		}
	}

	public static void InitFolder()
	{
		if( !Directory.Exists( BaseFolderAssetPath ) ) {
			Directory.CreateDirectory( BaseFolderAssetPath );
			AssetDatabase.ImportAsset( BaseFolderAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate );
		}
	}

	public static void InitMaterial_Both()
	{
		if( !File.Exists( MaterialAssetPath_Both ) ) {
			Material material = new Material( Shader.Find( RendererShaderName ) );

			material.SetInt( "_Draw", 0 );
			material.shaderKeywords = new string[1] { "_DRAW_BOTH" };
	
			AssetDatabase.CreateAsset( material, MaterialAssetPath_Both );
		}
	}

	public static void InitMaterial_ForwardBackward()
	{
		if( !File.Exists( MaterialAssetPath_Forward ) ) {
			Material material = new Material( Shader.Find( RendererShaderName ) );

			material.SetInt( "_Draw", 1 );
			material.shaderKeywords = new string[1] { "_DRAW_FORWARD" };

			AssetDatabase.CreateAsset( material, MaterialAssetPath_Forward );
		}

		if( !File.Exists( MaterialAssetPath_Backward ) ) {
			Material material = new Material( Shader.Find( RendererShaderName ) );

			material.SetInt( "_Draw", 2 );
			material.shaderKeywords = new string[1] { "_DRAW_BACKWARD" };

			AssetDatabase.CreateAsset( material, MaterialAssetPath_Backward );
		}
	}
}


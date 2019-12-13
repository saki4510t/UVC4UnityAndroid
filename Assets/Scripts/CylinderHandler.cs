using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderHandler : MonoBehaviour
{
	public GameObject TextureSource;

	private Renderer mRenderer;

	// Start is called before the first frame update
	void Start()
    {
		// TextureSourceで指定したGameObjectに割り当てられているMaterialを
		// このスクリプトが割り当てられているGameObjectのRendererに割り当てる
		// =同じ内容が表示されるはず
		mRenderer = GetComponent<Renderer>();
		if (TextureSource != null)
		{
			var renderer = TextureSource.GetComponent<Renderer>();
			if ((renderer != null) && (renderer.material != null))
			{
				mRenderer.material = renderer.material;
				Console.WriteLine("set material");
			}
		}
	}

//	void Update()
//	{
//	}
}

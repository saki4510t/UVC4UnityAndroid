/*
 * Copyright (c) 2014 - 2026 t_saki@serenegiant.com 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{
	/**
	 * UVC機器の接続・切断イベント用インターフェース
	 */
	public interface IUVCDetectHandler
	{
		/**
		 * UVC機器が接続された
		 * @param device 接続されたUVC機器情報
		 * @return true: UVC機器を使う, false: UVC機器を使わない
		 */
		bool OnUVCAttachEvent(UVCDevice device);
		/**
		 * UVC機器が取り外された
		 * @param device 接続されたUVC機器情報
		 */
		void OnUVCDetachEvent(UVCDevice device);
	}

	/**
	 * UVC関係のイベントハンドリングインターフェース
	 */
	public interface IUVCDrawer : IUVCDetectHandler
	{
		/**
		 * IUVCDrawerが指定したUVC機器の映像を描画できるかどうかを取得
		 * @param device 接続されたUVC機器情報
		 */
		bool IsUVCEnabled(UVCDevice device);
		/**
		 * UVC機器からの映像取得を開始した
		 * @param device 接続されたUVC機器情報
		 * @param tex UVC機器からの映像を受け取るTextureオブジェクト
		 */
		void OnUVCStartEvent(UVCDevice device, Texture tex);
		/**
		 * UVC機器からの映像取得を終了した
		 * @param device 接続されたUVC機器情報
		 */
		void OnUVCStopEvent(UVCDevice device);

		/**
		 * IUVCDrawerが指定したUAC機器の音声を取得できるかどうかを取得
		 * @param device 接続されたUAC機器情報
		 */
		bool IsUACEnabled(UVCDevice device);
		/**
		 * UAC機器からの音声取得を開始した
		 * @param device 接続されたUVC機器情報
		 * @param audioClip UAC機器からの音声を受け取るAudioClipオブジェクト
		 */
		void OnUACStartEvent(UVCDevice device, AudioClip audioClip);
		/**
		 * UAC機器からの音声取得を終了した
		 * @param device 接続されたUVC機器情報
		 */
		void OnUACStopEvent(UVCDevice device);
	}   // interface IUVCDrawer

}	// namespace Serenegiant.UVC

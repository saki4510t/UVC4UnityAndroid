using UnityEngine;

namespace Serenegiant.UVC
{
	/**
	 * UVC関係のイベントハンドリングインターフェースのホルダー
	 * 実際のインターフェースはインナーインターフェースとして定義
	 */
	public static class UVCEventHandler
	{

		/**
		 * UVC機器イベント処理インターフェースのマーカー
		 */
		public interface IUVCHandler
		{

		}

		/**
		 * UVC機器が接続されたときのイベントインターフェース
		 */
		public interface IOnUVCAttachHandler : IUVCHandler
		{
			/**
			 * UVC機器が接続された
			 * @param manager 呼び出し元のUVCManager
			 * @param device 接続されたUVC機器情報
			 * @return true: UVC機器を使う, false: UVC機器を使わない
			 */
			bool OnUVCAttachEvent(UVCManager manager, UVCDevice device);

		} // interface OnAttachEventHandler

		/**
		 * UVC機器が取り外されたときのイベントインターフェース
		 */
		public interface IOnUVCDetachHandler : IUVCHandler
		{
			/**
			 * UVC機器が取り外された
			 * @param manager 呼び出し元のUVCManager
			 * @param device 接続されたUVC機器情報
			 */
			void OnUVCDetachEvent(UVCManager manager, UVCDevice device);

		} // interface IOnDetachEventHandler

		/**
		 * UVC機器がからの映像取得の際に解像度を選択するためのイベントインターフェース
		 */
		public interface IOnUVCSelectSizeHandler : IUVCHandler
		{
			/**
			 * UVC機器が取り外された
			 * @param manager 呼び出し元のUVCManager
			 * @param device 接続されたUVC機器情報
			 * @param formats: 対応解像度
			 * @return 選択した解像度, nullなら呼び出し元UVCManagerのデフォルト値を使う
			 */
			SupportedFormats.Size OnUVCSelectSize(UVCManager manager, UVCDevice device, SupportedFormats formats);

		} // interface IPreviewSizeSelect

		/**
		 * UVC機器からの映像取得を開始したときのイベントインターフェース
		 */
		public interface IOnUVCStartHandler : IUVCHandler
		{
			/**
			 * UVC機器からの映像取得を開始した
			 * @param manager 呼び出し元のUVCManager
			 * @param device 接続されたUVC機器情報
			 * @param tex UVC機器からの映像を受け取るTextureオブジェクト
			 */
			void OnUVCStartEvent(UVCManager manager, UVCDevice device, Texture tex);

		} // interface IOnStartPreviewEventHandler

		/**
		 * UVC機器からの映像取得を終了したときのイベントインターフェース
		 */
		public interface IOnUVCStopHandler : IUVCHandler
		{
			/**
			 * UVC機器からの映像取得を終了した
			 * @param manager 呼び出し元のUVCManager
			 * @param device 接続されたUVC機器情報
			 */
			void OnUVCStopEvent(UVCManager manager, UVCDevice device);

		} // interface IOnStopPreviewEventHandler

		/**
		 * UVCからの映像描画クラスを示すインターフェース
		 */
		public interface IUVCDrawer : IOnUVCStartHandler, IOnUVCStopHandler
		{

		}

	} // class UVCEventHandler

} // namespace Serenegiant.UVC
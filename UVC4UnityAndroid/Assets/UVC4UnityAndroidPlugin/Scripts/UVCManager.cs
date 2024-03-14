//#define ENABLE_LOG
/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */

using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
using UnityEngine.Android;
#endif

namespace Serenegiant.UVC
{
    [RequireComponent(typeof(AndroidUtils))]
    public class UVCManager : MonoBehaviour
    {
        private const string TAG = "UVCManager#";
        private const string FQCN_DETECTOR = "com.serenegiant.usb.DeviceDetectorFragment";

        //--------------------------------------------------------------------------------
        private const int FRAME_TYPE_MJPEG = 0x000007;
        private const int FRAME_TYPE_H264 = 0x000014;
        private const int FRAME_TYPE_H264_FRAME = 0x030011;

        //--------------------------------------------------------------------------------
        // Camera Terminal DescriptorのbmControlsフィールドのビットマスク
        private const UInt64 CTRL_SCANNING		= 0x00000001;	// D0:  Scanning Mode
        private const UInt64 CTRL_AE			= 0x00000002;	// D1:  Auto-Exposure Mode
        private const UInt64 CTRL_AE_PRIORITY	= 0x00000004;	// D2:  Auto-Exposure Priority
        private const UInt64 CTRL_AE_ABS		= 0x00000008;	// D3:  Exposure Time (Absolute)
        private const UInt64 CTRL_AE_REL		= 0x00000010;	// D4:  Exposure Time (Relative)
        private const UInt64 CTRL_FOCUS_ABS		= 0x00000020;	// D5:  Focus (Absolute)
        private const UInt64 CTRL_FOCUS_REL		= 0x00000040;	// D6:  Focus (Relative)
        private const UInt64 CTRL_IRIS_ABS		= 0x00000080;	// D7:  Iris (Absolute)
        private const UInt64 CTRL_IRIS_REL		= 0x00000100;	// D8:  Iris (Relative)
        private const UInt64 CTRL_ZOOM_ABS		= 0x00000200;	// D9:  Zoom (Absolute)
        private const UInt64 CTRL_ZOOM_REL		= 0x00000400;	// D10: Zoom (Relative)
        private const UInt64 CTRL_PANTILT_ABS	= 0x00000800;	// D11: PanTilt (Absolute)
        private const UInt64 CTRL_PAN_ABS		= 0x01000800;	// D11: PanTilt (Absolute)
        private const UInt64 CTRL_TILT_ABS		= 0x02000800;	// D11: PanTilt (Absolute)
        private const UInt64 CTRL_PANTILT_REL	= 0x00001000;	// D12: PanTilt (Relative)
        private const UInt64 CTRL_PAN_REL		= 0x01001000;	// D12: PanTilt (Relative)
        private const UInt64 CTRL_TILT_REL		= 0x02001000;	// D12: PanTilt (Relative)
        private const UInt64 CTRL_ROLL_ABS		= 0x00002000;	// D13: Roll (Absolute)
        private const UInt64 CTRL_ROLL_REL		= 0x00004000;	// D14: Roll (Relative)
        private const UInt64 CTRL_D15			= 0x00008000;	// D15: Reserved
        private const UInt64 CTRL_D16			= 0x00010000;	// D16: Reserved
        private const UInt64 CTRL_FOCUS_AUTO	= 0x00020000;	// D17: Focus, Auto
        private const UInt64 CTRL_PRIVACY		= 0x00040000;	// D18: Privacy
        private const UInt64 CTRL_FOCUS_SIMPLE	= 0x00080000;	// D19: Focus, Simple
        private const UInt64 CTRL_WINDOW		= 0x00100000;	// D20: Window
        private const UInt64 CTRL_ROI			= 0x00200000;	// D21: ROI
        private const UInt64 CTRL_D22			= 0x00400000;	// D22: Reserved
        private const UInt64 CTRL_D23			= 0x00800000;	// D23: Reserved

        // Processing Unit DescriptorのbmControlsフィールドのビットマスク
        private const UInt64 PU_BRIGHTNESS		= 0x00000001;	// D0: Brightness
        private const UInt64 PU_CONTRAST		= 0x00000002;	// D1: Contrast
        private const UInt64 PU_HUE				= 0x00000004;	// D2: Hue
        private const UInt64 PU_SATURATION		= 0x00000008;	// D3: Saturation
        private const UInt64 PU_SHARPNESS		= 0x00000010;	// D4: Sharpness
        private const UInt64 PU_GAMMA			= 0x00000020;	// D5: Gamma
        private const UInt64 PU_WB_TEMP			= 0x00000040;	// D6: White Balance Temperature
        private const UInt64 PU_WB_COMPO		= 0x00000080;	// D7: White Balance Component
        private const UInt64 PU_BACKLIGHT		= 0x00000100;	// D8: Backlight Compensation
        private const UInt64 PU_GAIN			= 0x00000200;	// D9: Gain
        private const UInt64 PU_POWER_LF		= 0x00000400;	// D10: Power Line Frequency
        private const UInt64 PU_HUE_AUTO		= 0x00000800;	// D11: Hue, Auto
        private const UInt64 PU_WB_TEMP_AUTO	= 0x00001000;	// D12: White Balance Temperature, Auto
        private const UInt64 PU_WB_COMPO_AUTO	= 0x00002000;	// D13: White Balance Component, Auto
        private const UInt64 PU_DIGITAL_MULT	= 0x00004000;	// D14: Digital Multiplier
        private const UInt64 PU_DIGITAL_LIMIT	= 0x00008000;	// D15: Digital Multiplier Limit
        private const UInt64 PU_AVIDEO_STD		= 0x00010000;	// D16: Analog Video Standard
        private const UInt64 PU_AVIDEO_LOCK		= 0x00020000;	// D17: Analog Video Lock Status
        private const UInt64 PU_CONTRAST_AUTO	= 0x00040000;	// D18: Contrast, Auto
        private const UInt64 PU_D19				= 0x00080000;	// D19: Reserved
        private const UInt64 PU_D20				= 0x00100000;	// D20: Reserved
        private const UInt64 PU_D21				= 0x00200000;	// D21: Reserved
        private const UInt64 PU_D22				= 0x00400000;	// D22: Reserved
        private const UInt64 PU_D23				= 0x00800000;   // D23: Reserved

        // プロセッシングユニットのコントロールタイプを識別するために最上位ビットを立てる
        private const UInt64 PU_MASK = 0x80000000;

        //--------------------------------------------------------------------------------
        private static readonly UInt64[] SUPPORTED_CTRLS = {
            CTRL_SCANNING,
            CTRL_AE,
            CTRL_AE_PRIORITY,
            CTRL_AE_ABS,
            //CTRL_AE_REL,
            CTRL_FOCUS_ABS,
            //CTRL_FOCUS_REL,
            CTRL_IRIS_ABS,
            //CTRL_IRIS_REL,
            CTRL_ZOOM_ABS,
            //CTRL_ZOOM_REL,
            //CTRL_PANTILT_ABS,
            CTRL_PAN_ABS,
            CTRL_TILT_ABS,
            //CTRL_PANTILT_REL,
            //CTRL_PAN_REL,
            //CTRL_TILT_REL,
            CTRL_ROLL_ABS,
            //CTRL_ROLL_REL,
            CTRL_FOCUS_AUTO,
            //CTRL_PRIVACY,
            //CTRL_FOCUS_SIMPLE,
            //CTRL_WINDOW,
            //CTRL_ROI,
        };
        private static readonly UInt64[] SUPPORTED_PROCS =
        {
            PU_BRIGHTNESS,
            PU_CONTRAST,
            PU_HUE,
            PU_SATURATION,
            PU_SHARPNESS,
            PU_GAMMA,
            PU_WB_TEMP,
            PU_WB_COMPO,
            PU_BACKLIGHT,
            PU_GAIN,
            PU_POWER_LF,
            PU_HUE_AUTO,
            PU_WB_TEMP_AUTO,
            PU_WB_COMPO_AUTO,
            PU_DIGITAL_MULT,
            PU_DIGITAL_LIMIT,
            PU_AVIDEO_STD,
            PU_AVIDEO_LOCK,
            PU_CONTRAST_AUTO,

        };
    
        //--------------------------------------------------------------------------------
        /**
		 * IUVCSelectorがセットされていないとき
		 * またはIUVCSelectorが解像度選択時にnullを
		 * 返したときのデフォルトの解像度(幅)
		*/
        public Int32 DefaultWidth = 1280;
		/**
		 * IUVCSelectorがセットされていないとき
		 * またはIUVCSelectorが解像度選択時にnullを
		 * 返したときのデフォルトの解像度(高さ)
		 */
		public Int32 DefaultHeight = 720;
		/**
		 * UVC機器とのネゴシエーション時に
		 * H.264を優先してネゴシエーションするかどうか
		 * Android実機のみ有効
		 * true:	H.264 > MJPEG > YUV
		 * false:	MJPEG > H.264 > YUV
		 */
		public bool PreferH264 = false;
		/**
		 * 可能な場合にUACから音声取得を行うかどうか
		 */
		public bool UACEnabled = false;
        /**
         * シーンレンダリングの前にUVC機器映像のテクスチャへのレンダリング要求を行うかどうか
         */
        public bool RenderBeforeSceneRendering = false;

		/**
		 * UVC関係のイベンドハンドラー
		 */
		[SerializeField, ComponentRestriction(typeof(IUVCDrawer))]
		public Component[] UVCDrawers;

		/**
		 * 使用中のカメラ情報を保持するホルダークラス
		 */
		public class CameraInfo
		{
			internal readonly UVCDevice device;
			internal Texture previewTexture;
            internal int frameType;
			internal volatile Int32 activeId;
			private Int32 currentWidth;
			private Int32 currentHeight;
            private bool isRenderBeforeSceneRendering;
            private bool isRendering;
			private Dictionary<UInt64, UVCCtrlInfo> ctrlInfos = new Dictionary<UInt64, UVCCtrlInfo>();

			internal CameraInfo(UVCDevice device)
			{
				this.device = device;
			}

			/**
			 * 機器idを取得
			 */
			public Int32 Id{
				get { return device.id;  }
			}
	
			/**
			 * 機器名を取得
			 */
			public string DeviceName
			{
				get { return device.name; }
			}

			/**
			 * ベンダーIDを取得
			 */
			public int Vid
			{
				get { return device.vid; }
			}

			/**
			 * プロダクトIDを取得
			 */
			public int Pid
			{
				get { return device.pid; }
			}

			/**
			 * 映像取得中かどうか
			 */
			public bool IsPreviewing
			{
				get { return (activeId != 0) && (previewTexture != null); }
			}

			/**
			 * 現在の解像度(幅)
			 * プレビュー中でなければ0
			 */
			public Int32 CurrentWidth
			{
				get { return currentWidth; }
			}

			/**
			 * 現在の解像度(高さ)
			 * プレビュー中でなければ0
			 */
			public Int32 CurrentHeight
			{
				get { return currentHeight; }
			}

			/**
			 * 現在の解像度を変更
			 * @param width
			 * @param height
			 */
			internal void SetSize(Int32 width, Int32 height)
			{
				currentWidth = width;
				currentHeight = height;
			}

			/**
			 * サポートしているUVCコントロール/プロセッシング機能の情報を更新する
			 */
			public void UpdateCtrls()
			{
				ctrlInfos.Clear();
				var ctrls = GetCtrlSupports(Id);
				foreach (UInt64 ctrl in SUPPORTED_CTRLS)
				{
					if ((ctrls & ctrl) == ctrl)
					{
						UVCCtrlInfo info = new UVCCtrlInfo();
						info.type = ctrl;
						if (GetCtrlInfo(Id, ref info) == 0)
						{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
							Console.WriteLine($"{TAG}ctrl({ctrl:X}):={info}");
#endif
							ctrlInfos.Add(info.type, info);
						}
					}
				}
				ctrls = GetProcSupports(Id);
				foreach (UInt64 ctrl in SUPPORTED_PROCS)
				{
					if ((ctrls & ctrl) == ctrl)
					{
						UVCCtrlInfo info = new UVCCtrlInfo();
						info.type = ctrl | PU_MASK;
						if (GetCtrlInfo(Id, ref info) == 0)
						{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
							Console.WriteLine($"{TAG}proc({ctrl:X}):={info}");
#endif
							ctrlInfos.Add(info.type, info);
						}
					}
				}
			}

			/**
			 * 対応しているUVCコントロール/プロセッシング機能のtype一覧を取得
			 */
			public List<UInt64> GetCtrls()
			{
				return new List<UInt64>(ctrlInfos.Keys);
			}

			/**
			 * 指定したUVCコントロール/プロセッシング機能の情報を取得
			 * @param type
			 * @return UVCCtrlInfo
			 * @throws ArgumentOutOfRangeException
			 */
			public UVCCtrlInfo GetInfo(UInt64 type)
			{
				if (ctrlInfos.ContainsKey(type))
				{
					return ctrlInfos.GetValueOrDefault(type, new UVCCtrlInfo());
				} else
				{
					throw new ArgumentOutOfRangeException($"Not supported control type{type:X}");
				}
			}

			/**
			 * UVCコントロール/プロセッシング機能の設定値を取得
			 * @param type
			 * @return 変更後の値
			 * @throws ArgumentOutOfRangeException
			 * @throws Exception
			 */
			public Int32 GetValue(UInt64 type)
			{
				if (ctrlInfos.ContainsKey(type))
				{
					Int32 value = 0;
					var r = GetCtrlValue(Id, type, ref value);
					if (r == 0)
					{
						return value;
					} else
					{
						throw new Exception($"Failed to get control value,type={type},err={r}");
					}
				} else
				{
					throw new ArgumentOutOfRangeException($"Not supported control type{type:X}");
				}
			}

			/**
			 * UVCコントロール/プロセッシング機能の設定変更
			 * @param type
			 * @param value
			 * @return 変更後の値
			 * @throws ArgumentOutOfRangeException
			 * @throws Exception
			 */
			public Int32 SetValue(UInt64 type, Int32 value)
			{
				if (ctrlInfos.ContainsKey(type))
				{
					var r = SetCtrlValue(Id, type, value);
					if (r == 0)
					{
						r = GetCtrlValue(Id, type, ref value);
						if (r == 0)
						{
							var info = ctrlInfos.GetValueOrDefault(type, new UVCCtrlInfo());
							info.current = value;
							ctrlInfos[type] = info;
							return value;
						}
						else
						{
							throw new Exception($"Failed to get control value,type={type},err={r}");
						}
					}
					else
					{
						throw new Exception($"Failed to set control value,type={type},err={r}");
					}
				}
				else
				{
					throw new ArgumentOutOfRangeException($"Not supported control type{type:X}");
				}
			}

			public override string ToString()
			{
				return $"{base.ToString()}({currentWidth}x{currentHeight},id={Id},activeId={activeId},IsPreviewing={IsPreviewing})";
			}

            /**
             * UVC機器からの映像のレンダリングを開始
             * @param manager
             */
            internal Coroutine StartRender(UVCManager manager, bool renderBeforeSceneRendering)
            {
                StopRender(manager);
                isRenderBeforeSceneRendering = renderBeforeSceneRendering;
                isRendering = true;
                if (renderBeforeSceneRendering)
                {
                    return manager.StartCoroutine(OnRenderBeforeSceneRendering());
                } else
                {
                    return manager.StartCoroutine(OnRender());
                }
            }

            /**
             * UVC機器からの映像のレンダリングを終了
             * @param manager
             */
            internal void StopRender(UVCManager manager)
            {
                if (isRendering)
                {
                    isRendering = false;
                    if (isRenderBeforeSceneRendering)
                    {
                        manager.StopCoroutine(OnRenderBeforeSceneRendering());
                    }
                    else
                    {
                        manager.StopCoroutine(OnRender());
                    }
                }
            }

            /**
			 * レンダーイベント処理用
			 * コールーチンとして実行される
             * シーンレンダリングの前にUVC機器からの映像をテクスチャへレンダリング要求する
			 */
            private IEnumerator OnRenderBeforeSceneRendering()
			{
				var renderEventFunc = GetRenderEventFunc();
				for (; activeId != 0;)
				{
					yield return null;
					GL.IssuePluginEvent(renderEventFunc, activeId);
				}
				yield break;
			}

            /**
             * レンダーイベント処理用
             * コールーチンとして実行される
             * レンダリング後にUVC機器からの映像をテクスチャへレンダリング要求する
             */
            private IEnumerator OnRender()
            {
                var renderEventFunc = GetRenderEventFunc();
                for (; activeId != 0;)
                {
                    yield return new WaitForEndOfFrame();
                    GL.IssuePluginEvent(renderEventFunc, activeId);
                }
                yield break;
            }

		} // CameraInfo

		/**
		 * UAC機器からの音声取得に関するオブジェクトを保持するためのホルダークラス
		 */
		public class AudioInfo
		{
			internal readonly UVCDevice device;
			private UACInfo info = new UACInfo();
			private int samplesPerFrame = 0;
			private Int16[] buffer;	// 今のところPCM16にしか対応しない, OnPCM16Readないではなくここで保持するともしかするとメモリーブロックが不連続になったり移動したりするかもしれないけど
			private volatile AudioClip audioClip;
			private volatile Int32 activeId;

			internal AudioInfo(UVCDevice device)
			{
				this.device = device;
			}


			/**
			 * 音声取得中かどうか
			 */
			public bool IsStreaming
			{
				get { return (activeId != 0) && (audioClip != null); }
			}

			/**
			 * 音声取得開始する
			 * すでに音声取得中なら何もしない
			 * @param manager
			 */
			internal AudioClip Start(UVCManager manager)
			{
				var result = StartUAC(device.id);
				if (result == 0)
				{
					if (GetUACInfo(device.id, ref info) == 0)
					{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
						Console.WriteLine($"{TAG}Start:info={info}");
#endif
						try
						{
							// PCM16のはず
							activeId = device.id;
							samplesPerFrame = info.packetBytes / (info.resolution / 8);
							buffer = new Int16[samplesPerFrame];
							audioClip = AudioClip.Create(device.name, Int32.MaxValue, info.channels, info.samplingFreq, true, OnPCM16Read);
							return audioClip;
						}
						catch (Exception e)
						{
							activeId = 0;
#if (!NDEBUG && DEBUG && ENABLE_LOG)
							Console.WriteLine($"Failed to create audio clip,err={e}");
#endif
							throw e;
						}
					}
					else
					{
						manager.StopAudio(device);
						throw new Exception($"Failed to get streaming info,err={result}");
					}
				}
				else
				{
					throw new Exception($"Failed to start uac streaming,err={result}");
				}
			}

			/**
			 * 音声取得停止する
			 * @param manager
			 */
			internal void Stop(UVCManager manager)
			{
				activeId = 0;
				audioClip = null;
				manager.StopAudio(device);
			}

#if (!NDEBUG && DEBUG && ENABLE_LOG)
			private int readCnt = 0;
#endif

			/**
			 * AudioClipからのコールバック
			 * @param data
			 */
			private void OnPCM16Read(float[] data)
			{
				if (!IsStreaming) return;

				var numSamples = data.Length;					// 今回読み取る最大サンプル数
				var maxReadCnt = numSamples / samplesPerFrame;	// 最大読み込み回数
				if (maxReadCnt == 0)
				{
					maxReadCnt = 1;
				}
				var result = -1;
				Int64 ptsUs = 0;
				//var buffer = new Int16[samplesPerFrame];	// 高速化のためにStartでアロケーションするように変更した
				Int32 dataBytes = 0;
				Int32 totalSamples = 0;
				for (int i = 0; (i < maxReadCnt) && (totalSamples < numSamples); i++)
				{
					result = GetUACFrame(device.id, buffer, ref dataBytes, ref ptsUs);
					if ((result == 0) && (dataBytes >= 2))
					{
						var samples = Math.Min(dataBytes / 2, samplesPerFrame);
						for (int j = 0; j < samples; j++)
						{
							data[j + totalSamples] = buffer[j] / (float)short.MaxValue;
						}
						totalSamples += samples;
					}
					else
					{
						break;
					}
				}
				if (totalSamples < numSamples)
				{
					Array.Resize<float>(ref data, totalSamples);
				}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				if ((readCnt++ % 100) == 0)
				{
					Console.WriteLine($"{TAG}OnPCM16Read:len={numSamples},total={totalSamples},r={result},bytes={dataBytes},pts={ptsUs}");
				}
#endif
			}
		} // AudioInfo

		/**
		 * メインスレッド上で実行するためのSynchronizationContextインスタンス
		 */
		private SynchronizationContext mainContext;
		/**
		 * 端末に接続されたUVC機器の状態が変化した時のイベントコールバックを受け取るデリゲーター
		 */
		private PluginCallbackManager.OnDeviceChangedFunc callback;
		/**
		 * 端末に接続されたUVC機器リスト
		 */
		private List<UVCDevice> attachedDevices = new List<UVCDevice>();
		/**
		 * 映像取得中のUVC機器のマップ
		 * 機器識別用のid - CameraInfoペアを保持する
		 */
		private Dictionary<Int32, CameraInfo> cameraInfos = new Dictionary<int, CameraInfo>();

		/**
		 * 音声取得中のUVC機器のマップ
		 * 機器識別用のid - AudioInfoペアを保持する
		 */
		private Dictionary<Int32, AudioInfo> audioInFos = new Dictionary<int, AudioInfo>();

		//--------------------------------------------------------------------------------
		// UnityEngineからの呼び出し
		//--------------------------------------------------------------------------------
		// Start is called before the first frame update
		IEnumerator Start()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Start:");
#endif
			mainContext = SynchronizationContext.Current;
            callback = PluginCallbackManager.Add(this);
	
			yield return Initialize();
		}

#if (!NDEBUG && DEBUG && ENABLE_LOG)
		void OnApplicationFocus()
		{
			Console.WriteLine($"{TAG}OnApplicationFocus:");
		}
#endif

#if (!NDEBUG && DEBUG && ENABLE_LOG)
		void OnApplicationPause(bool pauseStatus)
		{
			Console.WriteLine($"{TAG}OnApplicationPause:{pauseStatus}");
		}
#endif

#if (!NDEBUG && DEBUG && ENABLE_LOG)
		void OnApplicationQuits()
		{
			Console.WriteLine($"{TAG}OnApplicationQuits:");
		}
#endif

		void OnDestroy()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnDestroy:");
#endif
			StopAll();
            PluginCallbackManager.Remove(this);
		}

		//--------------------------------------------------------------------------------
		// UVC機器接続状態が変化したときのプラグインからのコールバック関数
		//--------------------------------------------------------------------------------
        public void OnDeviceChanged(IntPtr devicePtr, bool attached)
        {
            var id = UVCDevice.GetId(devicePtr);
#if (!NDEBUG && DEBUG && ENABLE_LOG)
            Console.WriteLine($"{TAG}OnDeviceChangedInternal:id={id},attached={attached}");
#endif
            if (attached)
            {
                UVCDevice device = new UVCDevice(devicePtr);
#if (!NDEBUG && DEBUG && ENABLE_LOG)
                Console.WriteLine($"{TAG}OnDeviceChangedInternal:device={device.ToString()}");
#endif
                if (HandleOnAttachEvent(device))
                {
                    attachedDevices.Add(device);
                    StartPreview(device);
					if (UACEnabled)
					{	// UVCManagerのUAC機能が有効な場合
						StartAudio(device);
					}
				}
            }
            else
            {
                var found = attachedDevices.Find(item =>
                {
                    return item != null && item.id == id;
                });
                if (found != null)
                {
                    HandleOnDetachEvent(found);
                    StopPreview(found);
					StopAudio(found);
					RemoveCamera(found);
					RemoveAudio(found);
                    attachedDevices.Remove(found);
                }
            }
        }

		//================================================================================
		/**
		 * 接続中のUVC機器一覧を取得
		 * @return 接続中のUVC機器一覧List
		 */
		public List<CameraInfo> GetAttachedDevices()
		{
			return new List<CameraInfo>(cameraInfos.Values);
		}

//		/**
//		 * 対応解像度を取得
//		 * @param camera 対応解像度を取得するUVC機器を指定
//		 * @return 対応解像度 既にカメラが取り外されている/closeしているのであればnull
//		 */
//		public SupportedFormats GetSupportedVideoSize(CameraInfo camera)
//		{
//			var info = (camera != null) ? GetCamera(camera.device) : null;
//			if ((info != null) && info.IsOpen)
//			{
//				return GetSupportedVideoSize(info.DeviceName);
//			}
//			else
//			{
//				return null;
//			}
//		}

//		/**
//		 * 解像度を変更
//		 * @param 解像度を変更するUVC機器を指定
//		 * @param 変更する解像度を指定, nullならデフォルトに戻す
//		 * @param 解像度が変更されたかどうか
//		 */
//		public bool SetVideoSize(CameraInfo camera, SupportedFormats.Size size)
//		{
//			var info = (camera != null) ? GetCamera(camera.device) : null;
//			var width = size != null ? size.Width : DefaultWidth;
//			var height = size != null ? size.Height : DefaultHeight;
//			if ((info != null) && info.IsPreviewing)
//			{
//				if ((width != info.CurrentWidth) || (height != info.CurrentHeight))
//				{   // 解像度が変更になるとき
//					StopPreview(info.DeviceName);
//					StartPreview(info.DeviceName, width, height);
//					return true;
//				}
//			}
//			return false;
//		}

		private void StartPreview(UVCDevice device)
		{
			var info = CreateCameraIfNotExist(device);
			if ((info != null) && !info.IsPreviewing) {

				int width = DefaultWidth;
				int height = DefaultHeight;

//				var supportedVideoSize = GetSupportedVideoSize(deviceName);
//				if (supportedVideoSize == null)
//				{
//					throw new ArgumentException("fauled to get supported video size");
//				}

//				// 解像度の選択処理
//				if ((UVCDrawers != null) && (UVCDrawers.Length > 0))
//				{
//					foreach (var drawer in UVCDrawers)
//					{
//						if ((drawer is IUVCDrawer) && ((drawer as IUVCDrawer).IsUVCEnabled(this, info.device)))
//						{
//							var size = (drawer as IUVCDrawer).OnUVCSelectSize(this, info.device, supportedVideoSize);
//#if (!NDEBUG && DEBUG && ENABLE_LOG)
//							Console.WriteLine($"{TAG}StartPreview:selected={size}");
//#endif
//							if (size != null)
//							{   // 一番最初に見つかった描画可能なIUVCDrawersがnull以外を返せばそれを使う
//								width = size.Width;
//								height = size.Height;
//								break;
//							}
//						}
//					}
//				}

				// FIXME 対応解像度の確認処理
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}StartPreview:({width}x{height}),id={device.id}");
#endif
				int[] frameTypes = {
                    PreferH264 ? FRAME_TYPE_H264 : FRAME_TYPE_MJPEG,
                    PreferH264 ? FRAME_TYPE_MJPEG : FRAME_TYPE_H264,
                };
                foreach (var frameType in frameTypes)
                {
                    if (Resize(device.id, frameType, width, height) == 0)
                    {
                        info.frameType = frameType;
                        break;
                    }
                }
                    
				info.SetSize(width, height);
				info.activeId = device.id;
				info.UpdateCtrls();
				mainContext.Post(__ =>
				{   // テクスチャの生成はメインスレッドで行わないといけない
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"{TAG}映像受け取り用テクスチャ生成:({width}x{height})");
#endif
					Texture2D tex = new Texture2D(
							width, height,
							TextureFormat.ARGB32,
							false, /* mipmap */
							true /* linear */);
					tex.filterMode = FilterMode.Point;
					tex.Apply();
					info.previewTexture = tex;
					var nativeTexPtr = info.previewTexture.GetNativeTexturePtr();
					Start(device.id, nativeTexPtr.ToInt32());
					HandleOnStartPreviewEvent(info);
					info.StartRender(this, RenderBeforeSceneRendering);
				}, null);
			}
		}

		private void StopPreview(UVCDevice device) {
			var info = GetCamera(device);
			if ((info != null) && info.IsPreviewing)
			{
				mainContext.Post(__ =>
				{
					HandleOnStopPreviewEvent(info);
					Stop(device.id);
					info.StopRender(this);
					info.SetSize(0, 0);
					info.activeId = 0;
				}, null);
			}
		}


		private void StartAudio(UVCDevice device)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}StartAudio:");
#endif
			if (device.isUAC)
			{
				var audio = CreateAudioIfNotExist(device);
				if ((audio != null) && !audio.IsStreaming)
				{
					mainContext.Post(__ =>
					{
						var audioClip = audio.Start(this);
						HandleOnStartAudioEvent(audio, audioClip);
					}, null);

				}
			}
			else
			{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}StartAudio:Not a UAC device");
#endif
			}
		}

		private void StopAudio(UVCDevice device)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}StopAudio:");
#endif
			var audio = GetAudio(device);
			if (audio != null && audio.IsStreaming)
			{
				mainContext.Post(__ =>
				{
					HandleOnStopAudioEvent(audio);
					audio.Stop(this);
				}, null);
			}
		}

		private void StopAll() {
			List<CameraInfo> values = new List<CameraInfo>(cameraInfos.Values);
			foreach (var info in values)
			{
				StopAudio(info.device);
				StopPreview(info.device);
			}
		}

		//--------------------------------------------------------------------------------
		/**
		 * UVC機器が接続されたときの処理の実体
		 * @param info
		 * @return true: 接続されたUVC機器を使用する, false: 接続されたUVC機器を使用しない
		 */
		private bool HandleOnAttachEvent(UVCDevice device/*NonNull*/)
		{
			if ((UVCDrawers == null) || (UVCDrawers.Length == 0))
			{   // IUVCDrawerが割り当てられていないときはtrue(接続されたUVC機器を使用する)を返す
				return true;
			}
			else
			{
				bool hasDrawer = false;
				foreach (var drawer in UVCDrawers)
				{
					if (drawer is IUVCDrawer)
					{
						hasDrawer = true;
						if ((drawer as IUVCDrawer).OnUVCAttachEvent(this, device))
						{   // どれか1つのIUVCDrawerがtrueを返せばtrue(接続されたUVC機器を使用する)を返す
							return true;
						}
					}
				}
				// IUVCDrawerが割り当てられていないときはtrue(接続されたUVC機器を使用する)を返す
				return !hasDrawer;
			}
		}

		/**
		 * UVC機器が取り外されたときの処理の実体
		 * @param info
		 */
		private void HandleOnDetachEvent(UVCDevice device/*NonNull*/)
		{
			if ((UVCDrawers != null) && (UVCDrawers.Length > 0))
			{
				foreach (var drawer in UVCDrawers)
				{
					if (drawer is IUVCDrawer)
					{
						(drawer as IUVCDrawer).OnUVCDetachEvent(this, device);
					}
				}
			}
		}

		/**
		 * UVC機器からの映像取得を開始した
		 * @param args UVC機器の識別文字列
		 */
		void HandleOnStartPreviewEvent(CameraInfo camera)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStartPreviewEvent:({camera})");
#endif
			if ((camera != null) && camera.IsPreviewing && (UVCDrawers != null))
			{
				foreach (var drawer in UVCDrawers)
				{
					if ((drawer is IUVCDrawer) && (drawer as IUVCDrawer).IsUVCEnabled(this, camera.device))
					{
						(drawer as IUVCDrawer).OnUVCStartEvent(this, camera.device, camera.previewTexture);
					}
				}
			} else {
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}HandleOnStartPreviewEvent:No UVCDrawers");
#endif
			}
		}

		/**
		 * UVC機器からの映像取得を終了した
		 * @param args UVC機器の識別文字列
		 */
		void HandleOnStopPreviewEvent(CameraInfo camera)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStopPreviewEvent:({camera})");
#endif
			if (UVCDrawers != null)
			{
				foreach (var drawer in UVCDrawers)
				{
					if ((drawer is IUVCDrawer) && (drawer as IUVCDrawer).IsUVCEnabled(this, camera.device))
					{
						(drawer as IUVCDrawer).OnUVCStopEvent(this, camera.device);
					}
				}
			}
		}

		void HandleOnStartAudioEvent(AudioInfo audio, AudioClip audioClip)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStartAudioEvent:({audio})");
#endif
			if ((audio != null) && audio.IsStreaming && (UVCDrawers != null))
			{
				foreach (var drawer in UVCDrawers)
				{
					if ((drawer is IUVCDrawer) && (drawer as IUVCDrawer).IsUACEnabled(this, audio.device))
					{   // IsUACEnabledがtrueを返したIUVCDrawerだけOnUACStartEventを呼び出す
						(drawer as IUVCDrawer).OnUACStartEvent(this, audio.device, audioClip);
					}
				}
			}
		}

		void HandleOnStopAudioEvent(AudioInfo audio)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}HandleOnStopAudioEvent:({audio})");
#endif
			if (UVCDrawers != null)
			{
				foreach (var drawer in UVCDrawers)
				{
					if ((drawer is IUVCDrawer) && (drawer as IUVCDrawer).IsUACEnabled(this, audio.device))
					{   // IsUACEnabledがtrueを返したIUVCDrawerだけOnUACStopEventを呼び出す
						(drawer as IUVCDrawer).OnUACStopEvent(this, audio.device);
					}
				}
			}
		}

		//--------------------------------------------------------------------------------
		/**
		 * 指定したUVC識別文字列に対応するCameraInfoを取得する
		 * まだ登録させていなければ新規作成する
		 * @param deviceName UVC機器識別文字列
		 * @param CameraInfoを返す
		 */
		/*NonNull*/
		private CameraInfo CreateCameraIfNotExist(UVCDevice device)
		{
			if (!cameraInfos.ContainsKey(device.id))
			{
				cameraInfos[device.id] = new CameraInfo(device);
			}
			return cameraInfos[device.id];
		}

		/**
		 * 指定したUVC識別文字列に対応するCameraInfoを取得する
		 * @param device
		 * @param 登録してあればCameraInfoを返す、登録されていなければnull
		 */
		/*Nullable*/
		private CameraInfo GetCamera(UVCDevice device)
		{
			return cameraInfos.ContainsKey(device.id) ? cameraInfos[device.id] : null;
		}

		/**
		 * 指定したUVCDeviceに対応するCameraInfoを削除する
		 * @param device
		 * @param 対応するAudioInfoまたはnull
		 */
		private CameraInfo RemoveCamera(UVCDevice device)
		{
			var result = GetCamera(device);
			cameraInfos.Remove(device.id);

			return result;
		}

		/**
		 * 指定したUVC識別文字列に対応するCameraInfoを取得する
		 * まだ登録させていなければ新規作成する
		 * @param deviceName UVC機器識別文字列
		 * @param CameraInfoを返す
		 */
		/*NonNull*/
		private AudioInfo CreateAudioIfNotExist(UVCDevice device)
		{
			if (!audioInFos.ContainsKey(device.id))
			{
				audioInFos[device.id] = new AudioInfo(device);
			}
			return audioInFos[device.id];
		}

		/**
		 * 指定したUVCDeviceに対応するAudioInfoを取得する
		 * @param device
		 * @param 登録してあればAudioInfoを返す、登録されていなければnull
		 */
		/*Nullable*/
		private AudioInfo GetAudio(UVCDevice device)
		{
			return audioInFos.ContainsKey(device.id) ? audioInFos[device.id] : null;
		}

		/**
		 * 指定したUVCDeviceに対応するAudioInfoを削除する
		 * @param device
		 * @param 対応するAudioInfoまたはnull
		 */
		private AudioInfo RemoveAudio(UVCDevice device)
		{
			var result = GetAudio(device);
			audioInFos.Remove(device.id);
	
			return result;
		}

		//--------------------------------------------------------------------------------
		/**
		 * プラグインを初期化
		 * パーミッションの確認を行って取得できれば実際のプラグイン初期化処理#InitPluginを呼び出す
		 */
		private IEnumerator Initialize()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Initialize:");
#endif
			if (AndroidUtils.CheckAndroidVersion(28))
			{
				yield return AndroidUtils.GrantCameraPermission((string permission, AndroidUtils.PermissionGrantResult result) =>
				{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
					Console.WriteLine($"{TAG}OnPermission:{permission}={result}");
#endif
					switch (result)
					{
						case AndroidUtils.PermissionGrantResult.PERMISSION_GRANT:
							InitPlugin();
							break;
						case AndroidUtils.PermissionGrantResult.PERMISSION_DENY:
							if (AndroidUtils.ShouldShowRequestPermissionRationale(AndroidUtils.PERMISSION_CAMERA))
							{
								// パーミッションを取得できなかった
								// FIXME 説明用のダイアログ等を表示しないといけない
							}
							break;
						case AndroidUtils.PermissionGrantResult.PERMISSION_DENY_AND_NEVER_ASK_AGAIN:
							break;
					}
				});
			}
			else
			{
				InitPlugin();
			}

			yield break;
		}

		/**
		 * プラグインを初期化
		 * uvc-plugin-unityへの処理要求
		 */
		private void InitPlugin()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}InitPlugin:");
#endif
			// IUVCDrawersが割り当てられているかどうかをチェック
			var hasDrawer = false;
			if ((UVCDrawers != null) && (UVCDrawers.Length > 0))
			{
				foreach (var drawer in UVCDrawers)
				{
					if (drawer is IUVCDrawer)
					{
						hasDrawer = true;
						break;
					}
				}
			}
			if (!hasDrawer)
			{   // インスペクタでIUVCDrawerが設定されていないときは
				// このスクリプトがaddされているゲームオブジェクトからの取得を試みる
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine($"{TAG}InitPlugin:has no IUVCDrawer, try to get from gameObject");
#endif
				var drawers = GetComponents(typeof(IUVCDrawer));
				if ((drawers != null) && (drawers.Length > 0))
				{
					UVCDrawers = new Component[drawers.Length];
					int i = 0;
					foreach (var drawer in drawers)
					{
						UVCDrawers[i++] = drawer;
					}
				}
			}
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}InitPlugin:num drawers={UVCDrawers.Length}");
#endif
			// aandusbのDeviceDetectorを読み込み要求
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_DETECTOR))
			{
				clazz.CallStatic("initUVCDeviceDetector",
					AndroidUtils.GetCurrentActivity());
			}
		}

        //--------------------------------------------------------------------------------
        // ネイティブプラグイン関係の定義・宣言
        //--------------------------------------------------------------------------------
		/**
		 * プラグインでのレンダーイベント取得用native(c/c++)関数
		 */
		[DllImport("unityuvcplugin")]
		private static extern IntPtr GetRenderEventFunc();
        /**
		 * 初期設定
		 */
        [DllImport("unityuvcplugin", EntryPoint = "Config")]
        private static extern Int32 Config(Int32 deviceId, Int32 enabled, Int32 useFirstConfig);
        /**
		 * 映像取得開始
		 */
        [DllImport("unityuvcplugin", EntryPoint ="Start")]
		private static extern Int32 Start(Int32 deviceId, Int32 tex);
		/**
		 * 映像取得終了
		 */
		[DllImport("unityuvcplugin", EntryPoint ="Stop")]
		private static extern Int32 Stop(Int32 deviceId);
		/**
		 * 映像サイズ設定
		 */
		[DllImport("unityuvcplugin")]
		private static extern Int32 Resize(Int32 deviceId, Int32 frameType, Int32 width, Int32 height);
        /**
		 * 対応するUVCコントロール機能マスクを取得
		 */
        [DllImport("unityuvcplugin")]
        private static extern UInt64 GetCtrlSupports(Int32 deviceId);
        /**
		 * 対応するUVCプロセッシング機能マスクを取得
		 */
        [DllImport("unityuvcplugin")]
        private static extern UInt64 GetProcSupports(Int32 deviceId);
        /**
		 * 対応するUVCコントロール/プロセッシング機能情報を取得
		 */
        [DllImport("unityuvcplugin", CallingConvention=CallingConvention.StdCall)]
        private static extern Int32 GetCtrlInfo(Int32 deviceId, ref UVCCtrlInfo info);
        /**
		 * 対応するUVCコントロール/プロセッシング機能設定値を取得
		 */
        [DllImport("unityuvcplugin")]
        private static extern Int32 GetCtrlValue(Int32 deviceId, UInt64 ctrl, ref Int32 value);
        /**
		 * 対応するUVCコントロール/プロセッシング機能設定値を設定
		 */
        [DllImport("unityuvcplugin")]
        private static extern Int32 SetCtrlValue(Int32 deviceId, UInt64 ctrl, Int32 value);
        /**
		 * UACからの音声取得開始
		 */
        [DllImport("unityuvcplugin")]
        private static extern Int32 StartUAC(Int32 deviceId);
        /**
		 * UACからの音声取得終了
		 */
        [DllImport("unityuvcplugin")]
        private static extern Int32 StopUAC(Int32 deviceId);
        /**
		 * 対応するUVCコントロール/プロセッシング機能情報を取得
		 */
        [DllImport("unityuvcplugin", CallingConvention = CallingConvention.StdCall)]
        private static extern Int32 GetUACInfo(Int32 deviceId, ref UACInfo info);
		/**
		 * UACからの音声データを取得(呼び出し元スレッドを最大で500ミリ秒ブロックする)
		 */
		[DllImport("unityuvcplugin", CallingConvention = CallingConvention.StdCall)]
		private static extern Int32 GetUACFrame(Int32 deviceId, short[] data, ref Int32 dataLen, ref Int64 ptsUs);
	}   // UVCManager

	/**
     * IL2Cppだとc/c++からのコールバックにつかうデリゲーターをマーシャリングできないので
     * staticなクラス・関数で処理をしないといけない。
     * だだしそれだと呼び出し元のオブジェクトの関数を呼び出せないのでマネージャークラスを作成
     * とりあえずはUVCManagerだけを受け付けるのでインターフェースにはしていない
     */
	public static class PluginCallbackManager
    {
        //コールバック関数の型を宣言
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnDeviceChangedFunc(Int32 id, IntPtr devicePtr, bool attached);

        /**
		 * プラグインのnative側登録関数
		 */
        [DllImport("unityuvcplugin")]
        private static extern IntPtr Register(Int32 id, OnDeviceChangedFunc deviceChanged);
        /**
		 * プラグインのnative側登録解除関数
		 */
        [DllImport("unityuvcplugin")]
        private static extern IntPtr Unregister(Int32 id);

        private static Dictionary<Int32, UVCManager> sManagers = new Dictionary<Int32, UVCManager>();
  
        /**
         * 指定したUVCManagerを接続機器変化コールバックに追加
         */
        public static OnDeviceChangedFunc Add(UVCManager manager)
        {
            Int32 id = manager.GetHashCode();
			OnDeviceChangedFunc onDeviceChanged = new OnDeviceChangedFunc(OnDeviceChanged);
            sManagers.Add(id, manager);
            Register(id, onDeviceChanged);
            return onDeviceChanged;
        }

        /**
         * 指定したUVCManagerを接続機器変化コールバックから削除
         */
        public static void Remove(UVCManager manager)
        {
            Int32 id = manager.GetHashCode();
            Unregister(id);
            sManagers.Remove(id);
        }

        [MonoPInvokeCallback(typeof(OnDeviceChangedFunc))]
        public static void OnDeviceChanged(Int32 id, IntPtr devicePtr, bool attached)
        {
            var manager = sManagers.ContainsKey(id) ? sManagers[id] : null;
            if (manager != null)
            {
                manager.OnDeviceChanged(devicePtr, attached);
            }
        }

    } // PluginCallbackManager


}   // namespace Serenegiant.UVC

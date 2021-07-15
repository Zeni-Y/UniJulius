using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace UniJulius.Runtime
{

    public enum UniJuliusError
    {
        Success = 0,
        AlreadyDone = 1,
        
        NotBegin = -1,
        SrInstanceNotFound = -2,
        GrammarNotFound = -3,
        OutputFunctionIsNotDefined = -4,
        FailedLoadJconf = -5,
        FailedCreateRecog = -6,
        FailedCreateEngine = -7,
        FailedOpenStream = -8,
        DeviceError = -9,
        Unknown = -10
    }
    
    public static class UniJuliusCore
    {
        private const string DllName = "julius-native";
        private const int ClipLengthSeconds = 10;
        private const int SamplingRate = 16000;
        private static bool debugMode = false;

        public static event Action<List<RecognitionResult>> ResultReceived;
        

        // コールバックのインスタンスを保持しておかないと GC されてしまう
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DebugLogStrDelegate(string message);
        private static DebugLogStrDelegate debugLogStrCallback;
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DebugLogStrIntDelegate(string message, int num);
        private static DebugLogStrIntDelegate debugLogStrIntCallback;
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputResultDelegate(IntPtr resultPtr, int length);
        private static OutputResultDelegate outputResultCallback;
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr AudioReadCallback(int requestedBytes, IntPtr len);
        private static AudioReadCallback audioCallback;

        private static UnitySynchronizationContext context;
        private static AudioClip clip;

        // static にしないとうまくデータを共有できない
        private static float[] samplesFromMicrophone;
        private static Int16[] samplesInInt16;
        private static int previousReadPos;
        private static int numSamplesToCopy;
        private static IntPtr sharedBuffer;
        
        [DllImport(DllName)]
        private static extern void set_debug_log_str_func(DebugLogStrDelegate debugLogStr);
        [DllImport(DllName)]
        private static extern void set_debug_log_str_int_func(DebugLogStrIntDelegate debugLogStrInt);
        [DllImport(DllName)]
        private static extern void set_audio_callback(AudioReadCallback callback);
        [DllImport(DllName)]
        private static extern void set_result_func(OutputResultDelegate outputResult);
        /// <summary>
        /// 音声認識処理の初期化
        /// </summary>
        public static void Init(bool debug = false)
        {
            debugMode = debug;
            context = UnitySynchronizationContext.Create();
            clip = Microphone.Start("", true, ClipLengthSeconds, SamplingRate);
            samplesFromMicrophone = new float[clip.samples * clip.channels];
            samplesInInt16 = new Int16[clip.samples * clip.channels];
            sharedBuffer = Marshal.AllocCoTaskMem(clip.samples * clip.channels * 2);
            
            debugLogStrCallback = DebugLogStr;
            debugLogStrIntCallback = DebugLogStrInt;
            set_debug_log_str_func(debugLogStrCallback);
            set_debug_log_str_int_func(debugLogStrIntCallback);
            
            audioCallback = ReadAudio;
            outputResultCallback = OutputResult;
            set_audio_callback(audioCallback);
            set_result_func(outputResultCallback);
        }

        [DllImport(DllName)]
        private static extern int begin(string filename);
        /// <summary>
        /// 音声認識を開始する
        /// </summary>
        /// <param name="iRecognized"></param>
        public static UniJuliusError Begin(IRecognized iRecognized)
        {
            //JconfPath はmain threadからしか呼べないため、ここで参照しておく。
            var jconfPath = iRecognized.JconfPath;
            
            var error = UniJuliusError.NotBegin;
            var thread = new Thread(() =>
            {
                error = (UniJuliusError)begin(jconfPath);
                Debug.Log("Thread Finished");
            });
            thread.Start();
            
            return error;
        }
        
        /// <summary>
        /// Jconfファイルへのパスを直接指定して音声認識処理を開始する
        /// </summary>
        /// <param name="jconfPath">jconfファイルへのパス</param>
        public static UniJuliusError Begin(string jconfPath)
        {
            var error = UniJuliusError.NotBegin;
            var thread = new Thread(() =>
            {
                error = (UniJuliusError)begin(jconfPath);
                Debug.Log("Thread Finished");
            });
            thread.Start();

            return error;
        }
        
        [DllImport(DllName)]
        private static extern int finish();
        /// <summary>
        /// 音声認識処理を終了する
        /// </summary>
        public static UniJuliusError Finish()
        {
            var error = (UniJuliusError)finish();
            context.Update();

            if (sharedBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(sharedBuffer);
                sharedBuffer = IntPtr.Zero;
            }
            
            return error;
        }

        [DllImport(DllName)]
        private static extern int pause();
        /// <summary>
        /// 音声認識処理を中断する
        /// </summary>
        public static UniJuliusError Pause()
        {
            return (UniJuliusError)pause();
        }

        [DllImport(DllName)]
        private static extern int resume();
        /// <summary>
        /// 音声認識処理を再開する
        /// </summary>
        public static UniJuliusError Resume()
        { 
            return (UniJuliusError)resume();
        }
        
        [DllImport(DllName)]
//        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern int is_UniJulius_active();
        /// <summary>
        /// 音声認識処理の状態取得
        /// </summary>
        /// <returns>1:active,0:inactive,-1:音声認識処理が始まっていない</returns>
        public static int IsUniJuliusActive()
        {
            return is_UniJulius_active();
        }

        [DllImport(DllName)]
        private static extern int activate_sr_instance(string srInstanceName);
        /// <summary>
        /// 認識処理インスタンスを有効化する
        /// 登録されていない認識処理インスタンスの場合は何もしない
        /// </summary>
        /// <param name="srInstanceName">有効化する認識処理インスタンス名</param>
        /// <returns></returns>
        public static UniJuliusError ActivateSrInstance(string srInstanceName)
        {
            return (UniJuliusError)activate_sr_instance(srInstanceName);
        }
        
        [DllImport(DllName)] 
        private static extern int deactivate_sr_instance(string srInstanceName);
        /// <summary>
        /// 認識処理インスタンスを無効化する
        /// </summary>
        /// <param name="srInstanceName">無効化する認識処理インスタンス名</param>
        /// <returns></returns>
        public static UniJuliusError DeactivateSrInstance(string srInstanceName)
        {
            return (UniJuliusError)deactivate_sr_instance(srInstanceName);
        }
        
        [DllImport(DllName)]
        private static extern int activate_grammar(string srInstanceName, string grammarName,string dictPath, string dfaPath);
        /// <summary>
        /// 指定した文法を音声認識の対象として有効化する
        /// 文法が登録されていなければ登録する
        /// </summary>
        /// <param name="srInstanceName">有効化先の認識処理インスタンス名</param>
        /// <param name="grammarName">有効化する文法の名称。</param>
        /// <param name="dictPath">辞書ファイルへのパス</param>
        /// <param name="dfaPath">文法ファイルへのパス</param>
        /// <returns></returns>
        public static UniJuliusError ActivateGrammar(string srInstanceName, string grammarName, string dictPath, string dfaPath)
        {
            return (UniJuliusError)activate_grammar(srInstanceName,grammarName, dictPath ,dfaPath);
        }

        [DllImport(DllName)]
        private static extern int deactivate_grammar(string srInstanceName, string grammarName);
        /// <summary>
        /// 指定した文法を音声認識の対象外にする
        /// </summary>
        /// <param name="srInstanceName">無効化先の認識処理インスタンス名</param>
        /// <param name="grammarName">無効化する文法の名称</param>
        /// <returns></returns>
        public static UniJuliusError DeactivateGrammar(string srInstanceName, string grammarName)
        {
            return (UniJuliusError)deactivate_grammar(srInstanceName, grammarName);
        }

        [DllImport(DllName)]
        private static extern int add_grammar(string srInstanceName, string grammarName, string dictPath, string dfaPath);
        /// <summary>
        /// 認識処理インスタンスに文法を追加する（音声認識の対象には追加しない）
        /// </summary>
        /// <param name="srInstanceName">追加先の認識処理インスタンス名</param>
        /// <param name="grammarName">追加する文法の名称</param>
        /// <param name="dictPath">辞書ファイルへのパス</param>
        /// <param name="dfaPath">文法ファイルへのパス</param>
        /// <returns></returns>
        public static UniJuliusError AddGrammar(string srInstanceName, string grammarName, string dictPath, string dfaPath)
        {
            return (UniJuliusError)add_grammar(srInstanceName, grammarName, dictPath, dfaPath);
        }

        [DllImport(DllName)]
        private static extern int delete_grammar(string srInstanceName, string grammarName);
        /// <summary>
        /// 認識処理インスタンスの文法を削除
        /// </summary>
        /// <param name="srInstanceName">削除元の認識処理インスタンス名</param>
        /// <param name="grammarName">削除する文法の名称</param>
        /// <returns></returns>
        public static int DeleteGrammar(string srInstanceName, string grammarName)
        {
            return delete_grammar(srInstanceName, grammarName);
        }

        [AOT.MonoPInvokeCallback(typeof(OutputResultDelegate))]
        private static void OutputResult(IntPtr resultStringPointer, int length)
        {
            var buffer = new byte[length];
            Marshal.Copy(resultStringPointer, buffer, 0, length);
            var result = USEncoder.ToEncoding.ToUnicode(buffer);
            var parsedResult = UniJuliusUtil.ParseResult(result);

            context.Post((r) =>
            {
                ResultReceived?.Invoke((List<RecognitionResult>)r);
            }, parsedResult);
        }

        [AOT.MonoPInvokeCallback(typeof(DebugLogStrDelegate))]
        private static void DebugLogStr(string msg)
        {
            if (debugMode)
                Debug.Log(msg);
        }

        [AOT.MonoPInvokeCallback(typeof(DebugLogStrIntDelegate))]
        private static void DebugLogStrInt(string msg, int num)
        {
            if (debugMode)
                Debug.Log(msg +":"+ num);
        }

    
        [AOT.MonoPInvokeCallback(typeof(AudioReadCallback))]
        private static IntPtr ReadAudio(int sampleNums, IntPtr numSamplesPointer)
        {
            context.Send((_) =>
            {
                if (sampleNums <= 0)
                {
                    numSamplesToCopy = 0;
                    return;
                }

                // 前回の録音位置から今回の録音位置までを返す
                var currentReadPos = Microphone.GetPosition("");
                if (previousReadPos < currentReadPos) // 巻き戻りが起きていない
                {
                    var sampleRected = (currentReadPos - previousReadPos);
                    numSamplesToCopy = Math.Min(sampleRected, sampleNums);

                    CopySamplesFromClipToSharedBuffer(numSamplesToCopy);
                }
                else if (currentReadPos < previousReadPos) // 巻き戻りが起きている
                {
                    var sampleRected = ((clip.samples - previousReadPos) + currentReadPos);
                    numSamplesToCopy = Math.Min(sampleRected, sampleNums);

                    CopySamplesFromClipToSharedBuffer(numSamplesToCopy);
                }
                else
                {
                    numSamplesToCopy = 0;
                }

                previousReadPos = currentReadPos;

            }, null);

            Marshal.WriteInt32(numSamplesPointer, numSamplesToCopy);
            return sharedBuffer;
        }

        private static void CopySamplesFromClipToSharedBuffer(int samplesToRead)
        {
            clip.GetData(samplesFromMicrophone, previousReadPos);

            samplesInInt16 = Array.ConvertAll(samplesFromMicrophone, (v) => (Int16)(Int16.MaxValue * (v * 0.5)));
            Marshal.Copy(samplesInInt16, 0, sharedBuffer, samplesToRead);
        }
    }
}

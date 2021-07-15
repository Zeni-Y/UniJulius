using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UniJulius = UniJulius.Runtime.UniJulius;
using UniJulius.Runtime;

namespace UniJulius.Sample
{
    public class UniJuliusSample : MonoBehaviour
    {
        [SerializeField] private Text debugText;
        [SerializeField] private bool isDebug;
        [SerializeField] private string addedTarget;
        [SerializeField] private SpellContainer spellContainer;
        [SerializeField] private IsolatedWordData isolatedWordData;
        [SerializeField] private SpellPacketData spellPacketData;
        private string path = "Assets/StreamingAssets/UniJulius/grammar-kit/spell/multi_spell.jconf";
        
//        IEnumerator Start()
//        {
//            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
//
//            UniJulius.ResultReceived += OnResultReceived;
//            UniJulius.Init(isDebug);
//            UniJulius.Begin(spellContainer);
////            UniJulius.Begin(isolatedWordData);
////            UniJulius.Begin(spellPacketData);
////            UniJulius.Begin(path, true);
//        }

        private void Start()
        {
            UniJuliusCore.ResultReceived += OnResultReceived;
            UniJuliusCore.Init(isDebug);
            UniJuliusCore.Begin(spellContainer);
//            UniJuliusCore.Begin(spellPacketData);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) 
                UniJuliusCore.Pause();
            if (Input.GetKeyDown(KeyCode.R))
                UniJuliusCore.Resume();
            if (Input.GetKeyDown(KeyCode.I))
                Debug.Log(UniJuliusCore.IsUniJuliusActive());
            if (Input.GetKeyDown(KeyCode.D))
                UniJuliusCore.DeactivateSrInstance("_default");
            if (Input.GetKeyDown(KeyCode.A))
                UniJuliusCore.ActivateSrInstance("_default");
            if (Input.GetKeyDown(KeyCode.W))
            {
                var tmp = UniJuliusUtil.GetDictPath(addedTarget);
                UniJuliusCore.AddGrammar("_default",addedTarget, tmp,null);
            }
            if (Input.GetKeyDown(KeyCode.E))
                UniJuliusCore.DeleteGrammar("_default",addedTarget);
            if (Input.GetKeyDown(KeyCode.F))
            {
                var tmp = UniJuliusUtil.GetDictPath(addedTarget);
                UniJuliusCore.ActivateGrammar("_default", addedTarget,tmp, null);
            }
            if (Input.GetKeyDown(KeyCode.G))
                UniJuliusCore.DeactivateGrammar("_default",addedTarget);
        }

        string lastResult = "";

        void OnResultReceived(List<RecognitionResult> result)
        {
            lastResult = "";
            foreach (var parsedResult in result)
            {
                lastResult += parsedResult.ToString() + "\n";
            }
            
            if (debugText != null)
                debugText.text = lastResult;
            else
                Debug.Log(lastResult);
        }

//        void OnGUI()
//        {
//            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), lastResult);
//        }
        
        void OnDestroy()
        {
            UniJuliusCore.Finish();
            Debug.Log("Finish!!!!");
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPTUnity.Actions
{
    [GPTAction]
    public class CreateMonoBehaviourAction : CreateFileActionBase
    {
        [GPTParameter("MonoBehaviour code to use")]
        public string ScriptCode { get; set; }

        public override string Content => ScriptCode;
    }
}
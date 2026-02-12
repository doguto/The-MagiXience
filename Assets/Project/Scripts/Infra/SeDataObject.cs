using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scripts.Infra
{
    [CreateAssetMenu(fileName = "SeData", menuName = "Database/SeData")]
    public class SeDataObject : ScriptableObject
    {
        public List<SeData> seData = new();
    }

    [Serializable]
    public class SeData
    {
        public string name;
        public SeType seType;
    }
}

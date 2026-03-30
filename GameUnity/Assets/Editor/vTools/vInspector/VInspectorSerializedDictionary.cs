using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;



namespace VInspector
{
    [System.Serializable]
    public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        public List<SerializedKeyValuePair<TKey, TValue>> serializedKvps = new();

        public float dividerPos = .33f;

        public void OnBeforeSerialize()
        {
            foreach (var kvp in this)
                if (serializedKvps.FirstOrDefault(r => this.Comparer.Equals(r.Key, kvp.Key)) is SerializedKeyValuePair<TKey, TValue> serializedKvp)
                    serializedKvp.Value = kvp.Value;
                else
                    serializedKvps.Add(kvp);

            serializedKvps.RemoveAll(r => r.Key is not null && !this.ContainsKey(r.Key));

            for (int i = 0; i < serializedKvps.Count; i++)
                serializedKvps[i].index = i;

        }
        public void OnAfterDeserialize()
        {
            this.Clear();

            foreach (var serializedKvp in serializedKvps)
            {
                serializedKvp.isKeyNull = serializedKvp.Key is null;
                serializedKvp.isKeyRepeated = serializedKvp.Key is not null && this.ContainsKey(serializedKvp.Key);

                if (serializedKvp.isKeyNull) continue;
                if (serializedKvp.isKeyRepeated) continue;


                this.Add(serializedKvp.Key, serializedKvp.Value);

            }

        }



        [System.Serializable]
        public class SerializedKeyValuePair<TKey_, TValue_>
        {
            public TKey_ Key;
            public TValue_ Value;

            public int index;

            public bool isKeyRepeated;
            public bool isKeyNull;


            public SerializedKeyValuePair(TKey_ key, TValue_ value) { this.Key = key; this.Value = value; }

            public static implicit operator SerializedKeyValuePair<TKey_, TValue_>(KeyValuePair<TKey_, TValue_> kvp) => new(kvp.Key, kvp.Value);
            public static implicit operator KeyValuePair<TKey_, TValue_>(SerializedKeyValuePair<TKey_, TValue_> kvp) => new(kvp.Key, kvp.Value);

        }

    }

}
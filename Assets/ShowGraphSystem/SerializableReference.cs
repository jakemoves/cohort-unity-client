using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShowGraphSystem
{
    [Serializable]
    public struct SerializableReference<T> where T : class
    {
        [SerializeReference]
        private T _value;

        public T Value => _value;

        public SerializableReference(T value) => _value = value;

        public static implicit operator T(SerializableReference<T> serializableReference) => serializableReference.Value;
        public static implicit operator SerializableReference<T>(T value) => new SerializableReference<T>(value);
    }
}

﻿using System.Runtime.Serialization;

namespace BabylonExport.Entities
{
    [DataContract]
    public class BabylonAbstractMesh
    {
        [DataMember]
        public string name { get; set; }
        
        [DataMember]
        public float[] position { get; set; }

        [DataMember]
        public float[] rotation { get; set; }

        [DataMember]
        public float[] scaling { get; set; }

        [DataMember]
        public float[] rotationQuaternion { get; set; }

        [DataMember]
        public BabylonAnimation[] animations { get; set; }

        public BabylonAbstractMesh()
        {
            position = new[] { 0f, 0f, 0f };
            rotation = new[] { 0f, 0f, 0f };
            scaling = new[] { 1f, 1f, 1f };
        }
    }
}

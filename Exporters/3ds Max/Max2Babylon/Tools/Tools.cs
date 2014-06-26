﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Max;
using SharpDX;

namespace Max2Babylon
{
    public static class Tools
    {
        public const float Epsilon = 0.001f;

        public static IPoint3 XAxis { get { return Loader.Global.Point3.Create(1, 0, 0); } }
        public static IPoint3 YAxis { get { return Loader.Global.Point3.Create(0, 1, 0); } }
        public static IPoint3 ZAxis { get { return Loader.Global.Point3.Create(0, 0, 1); } }
        public static IPoint3 Origin { get { return Loader.Global.Point3.Create(0, 0, 0); } }

        public static IInterval Forever
        {
            get { return Loader.Global.Interval.Create(int.MinValue, int.MaxValue); }
        }

        public static IMatrix3 Identity { get { return Loader.Global.Matrix3.Create(XAxis, YAxis, ZAxis, Origin); } }


        public static Vector3 ToEulerAngles(this IQuat q)
        {
            // Store the Euler angles in radians
            var pitchYawRoll = new Vector3();

            double sqw = q.W * q.W;
            double sqx = q.X * q.X;
            double sqy = q.Y * q.Y;
            double sqz = q.Z * q.Z;

            // If quaternion is normalised the unit is one, otherwise it is the correction factor
            double unit = sqx + sqy + sqz + sqw;
            double test = q.X * q.Y + q.Z * q.W;

            if (test > 0.4999f * unit)                              // 0.4999f OR 0.5f - EPSILON
            {
                // Singularity at north pole
                pitchYawRoll.Y = 2f * (float)Math.Atan2(q.X, q.W);  // Yaw
                pitchYawRoll.X = (float)Math.PI * 0.5f;             // Pitch
                pitchYawRoll.Z = 0f;                                // Roll
                return pitchYawRoll;
            }
            if (test < -0.4999f * unit)                        // -0.4999f OR -0.5f + EPSILON
            {
                // Singularity at south pole
                pitchYawRoll.Y = -2f * (float)Math.Atan2(q.X, q.W); // Yaw
                pitchYawRoll.X = -(float)Math.PI * 0.5f;            // Pitch
                pitchYawRoll.Z = 0f;                                // Roll
                return pitchYawRoll;
            }
            pitchYawRoll.Y = (float)Math.Atan2(2f * q.Y * q.W - 2f * q.X * q.Z, sqx - sqy - sqz + sqw);       // Yaw
            pitchYawRoll.X = (float)Math.Asin(2f * test / unit);                                             // Pitch
            pitchYawRoll.Z = (float)Math.Atan2(2f * q.X * q.W - 2f * q.Y * q.Z, -sqx + sqy - sqz + sqw);      // Roll

            return pitchYawRoll;
        }

        public static void PreparePipeline(IINode node, bool deactivate)
        {
            var obj = node.ObjectRef;

            if (obj == null || obj.SuperClassID != SClass_ID.GenDerivob)
            {
                return;
            }

            var derivedObject = obj as IIDerivedObject;

            if (derivedObject == null)
            {
                return;
            }

            for (var index = 0; index < derivedObject.NumModifiers; index++)
            {
                var modifier = derivedObject.GetModifier(index);

                //if (modifier.ClassID.PartA == 9815843 && modifier.ClassID.PartB == 87654) // Skin
                //{
                //    if (deactivate)
                //    {
                //        modifier.DisableMod();
                //    }
                //    else
                //    {
                //        modifier.EnableMod();
                //    }
                //}
            }
        }

        public static VNormal[] ComputeNormals(IMesh mesh, bool optimize)
        {
            var vnorms = new VNormal[mesh.NumVerts];
            var fnorms = new Vector3[mesh.NumFaces];

            for (var index = 0; index < mesh.NumVerts; index++)
            {
                vnorms[index] = new VNormal();
            }

            for (var index = 0; index < mesh.NumFaces; index++)
            {
                var face = mesh.Faces[index];
                Vector3 v0 = mesh.Verts[(int)face.V[0]].ToVector3();
                Vector3 v1 = mesh.Verts[(int)face.V[1]].ToVector3();
                Vector3 v2 = mesh.Verts[(int)face.V[2]].ToVector3();

                fnorms[index] = Vector3.Cross((v1 - v0), (v2 - v1));

                for (var j = 0; j < 3; j++)
                {
                    vnorms[(int)face.V[j]].AddNormal(fnorms[index], optimize ? 1 : face.SmGroup);
                }

                fnorms[index].Normalize();
            }

            for (var index = 0; index < mesh.NumVerts; index++)
            {
                vnorms[index].Normalize();
            }

            return vnorms;
        }

        public static bool IsEqualTo(this float[] value, float[] other)
        {
            if (value.Length != other.Length)
            {
                return false;
            }

            return !value.Where((t, i) => Math.Abs(t - other[i]) > Epsilon).Any();
        }

        public static float[] ToArray(this IMatrix3 value)
        {
            var row0 = value.GetRow(0).ToArraySwitched();
            var row1 = value.GetRow(1).ToArraySwitched();
            var row2 = value.GetRow(2).ToArraySwitched();
            var row3 = value.GetRow(3).ToArraySwitched();

            return new[]
            {
                row0[0], row0[1], row0[2], 0,
                row2[0], row2[1], row2[2], 0,
                row1[0], row1[1], row1[2], 0,
                row3[0], row3[1], row3[2], 1
            };
        }

        public static IPoint3 ToPoint3(this Vector3 value)
        {
            return Loader.Global.Point3.Create(value.X, value.Y, value.Z);
        }

        public static Vector3 ToVector3(this IPoint3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static Quaternion ToQuat(this IQuat value)
        {
            return new Quaternion(value.X, value.Z, value.Y, value.W);
        }
        public static float[] ToArray(this IQuat value)
        {
            return new[] { value.X, value.Z, value.Y, value.W };
        }

        public static float[] Scale(this IColor value, float scale)
        {
            return new[] { value.R * scale, value.G * scale, value.B * scale };
        }

        public static float[] ToArray(this IPoint4 value)
        {
            return new[] { value.X, value.Y, value.Z, value.W };
        }

        public static float[] ToArray(this IPoint3 value)
        {
            return new[] { value.X, value.Y, value.Z };
        }

        public static float[] ToArray(this IPoint2 value)
        {
            return new[] { value.X, value.Y };
        }

        public static float[] ToArraySwitched(this IPoint2 value)
        {
            return new[] { value.X, 1.0f - value.Y };
        }

        public static float[] ToArraySwitched(this IPoint3 value)
        {
            return new[] { value.X, value.Z, value.Y };
        }

        public static float[] ToArray(this IColor value)
        {
            return new[] { value.R, value.G, value.B };
        }

        public static IEnumerable<IINode> Nodes(this IINode node)
        {
            for (int i = 0; i < node.NumberOfChildren; ++i)
                if (node.GetChildNode(i) != null)
                    yield return node.GetChildNode(i);
        }

        public static IEnumerable<IINode> NodeTree(this IINode node)
        {
            foreach (var x in node.Nodes())
            {
                foreach (var y in x.NodeTree())
                    yield return y;
                yield return x;
            }
        }

        public static IEnumerable<IINode> NodesListBySuperClass(this IINode rootNode, SClass_ID sid)
        {
            return from n in rootNode.NodeTree() where n.ObjectRef != null && n.EvalWorldState(0, false).Obj.SuperClassID == sid select n;
        }

        public static IEnumerable<IINode> NodesListBySuperClasses(this IINode rootNode, SClass_ID[] sids)
        {
            return from n in rootNode.NodeTree() where n.ObjectRef != null && sids.Any(sid => n.EvalWorldState(0, false).Obj.SuperClassID == sid) select n;
        }

        public static float ConvertFov(float fov)
        {
            return (float)(2.0f * Math.Atan(Math.Tan(fov / 2.0f) / Loader.Core.ImageAspRatio));
        }

        public static bool HasParent(this IINode node)
        {
            return node.ParentNode != null && node.ParentNode.ObjectRef != null;
        }

        public static Guid GetGuid(this IAnimatable node)
        {
            var uidData = node.GetAppDataChunk(Loader.Class_ID, SClass_ID.Basenode, 0);
            Guid uid;

            if (uidData != null)
            {
                uid = new Guid(uidData.Data);
            }
            else
            {
                uid = Guid.NewGuid();
                node.AddAppDataChunk(Loader.Class_ID, SClass_ID.Basenode, 0, uid.ToByteArray());
            }

            return uid;
        }

        public static string GetLocalData(this IAnimatable node)
        {
            var uidData = node.GetAppDataChunk(Loader.Class_ID, SClass_ID.Basenode, 1);

            if (uidData != null)
            {
                return System.Text.Encoding.UTF8.GetString(uidData.Data);
            }

            return "";
        }

        public static void SetLocalData(this IAnimatable node, string value)
        {
            var uidData = node.GetAppDataChunk(Loader.Class_ID, SClass_ID.Basenode, 1);

            if (uidData != null)
            {
                node.RemoveAppDataChunk(Loader.Class_ID, SClass_ID.Basenode, 1);
            }

            node.AddAppDataChunk(Loader.Class_ID, SClass_ID.Basenode, 1, System.Text.Encoding.UTF8.GetBytes(value));
        }

        public static IMatrix3 GetWorldMatrix(this IINode node, int t, bool parent)
        {
            var tm = node.GetNodeTM(t, Forever);
            var ptm = node.ParentNode.GetNodeTM(t, Forever);

            if (!parent)
                return tm;

            if (node.ParentNode.SuperClassID == SClass_ID.Camera)
            {
                var r = ptm.GetRow(3);
                ptm.IdentityMatrix();
                ptm.SetRow(3, r);
            }

            ptm.Invert();
            return tm.Multiply(ptm);
        }

        public static IMatrix3 GetWorldMatrixComplete(this IINode node, int t, bool parent)
        {
            var tm = node.GetObjTMAfterWSM(t, Forever);
            var ptm = node.ParentNode.GetObjTMAfterWSM(t, Forever);

            if (!parent)
                return tm;

            if (node.ParentNode.SuperClassID == SClass_ID.Camera)
            {
                var r = ptm.GetRow(3);
                ptm.IdentityMatrix();
                ptm.SetRow(3, r);
            }

            ptm.Invert();
            return tm.Multiply(ptm);
        }

        public static ITriObject GetMesh(this IObject obj)
        {
            var triObjectClassId = Loader.Global.Class_ID.Create(0x0009, 0);

            if (obj.CanConvertToType(triObjectClassId) == 0)
                return null;

            return obj.ConvertToType(0, triObjectClassId) as ITriObject;
        }

        public static bool IsAlmostEqualTo(this IPoint4 current, IPoint4 other, float epsilon)
        {
            if (Math.Abs(current.X - other.X) > epsilon)
            {
                return false;
            }

            if (Math.Abs(current.Y - other.Y) > epsilon)
            {
                return false;
            }

            if (Math.Abs(current.Z - other.Z) > epsilon)
            {
                return false;
            }

            if (Math.Abs(current.W - other.W) > epsilon)
            {
                return false;
            }

            return true;
        }

        public static bool IsAlmostEqualTo(this IPoint3 current, IPoint3 other, float epsilon)
        {
            if (Math.Abs(current.X - other.X) > epsilon)
            {
                return false;
            }

            if (Math.Abs(current.Y - other.Y) > epsilon)
            {
                return false;
            }

            if (Math.Abs(current.Z - other.Z) > epsilon)
            {
                return false;
            }

            return true;
        }

        public static bool IsAlmostEqualTo(this IPoint2 current, IPoint2 other, float epsilon)
        {
            if (Math.Abs(current.X - other.X) > epsilon)
            {
                return false;
            }

            if (Math.Abs(current.Y - other.Y) > epsilon)
            {
                return false;
            }

            return true;
        }

        public static bool GetBoolProperty(this IINode node, string propertyName, int defaultState = 0)
        {
            int state = defaultState;
#if MAX2015
            node.GetUserPropBool(propertyName, ref state);
#else
            node.GetUserPropBool(ref propertyName, ref state);
#endif

            return state == 1;
        }

        public static float GetFloatProperty(this IINode node, string propertyName, float defaultState = 0)
        {
            float state = defaultState;
#if MAX2015
            node.GetUserPropFloat(propertyName, ref state);
#else
            node.GetUserPropFloat(ref propertyName, ref state);
#endif

            return state;
        }

        public static float[] GetVector3Property(this IINode node, string propertyName)
        {
            float state0 = 0;
            string name = propertyName + "_x";
#if MAX2015
            node.GetUserPropFloat(name, ref state0);
#else
            node.GetUserPropFloat(ref name, ref state0);
#endif


            float state1 = 0;
            name = propertyName + "_y";
#if MAX2015
            node.GetUserPropFloat(name, ref state1);
#else
            node.GetUserPropFloat(ref name, ref state1);
#endif

            float state2 = 0;
            name = propertyName + "_z";
#if MAX2015
            node.GetUserPropFloat(name, ref state2);
#else
            node.GetUserPropFloat(ref name, ref state2);
#endif

            return new[] { state0, state1, state2 };
        }

        public static bool PrepareCheckBox(CheckBox checkBox, IINode node, string propertyName, int defaultState = 0)
        {
            var state = node.GetBoolProperty(propertyName, defaultState);

            if (checkBox.CheckState == CheckState.Indeterminate)
            {
                checkBox.CheckState = state ? CheckState.Checked : CheckState.Unchecked;
            }
            else
            {
                if (!state && checkBox.CheckState == CheckState.Checked ||
                    state && checkBox.CheckState == CheckState.Unchecked)
                {
                    checkBox.CheckState = CheckState.Indeterminate;
                    return true;
                }
            }

            return false;
        }

        public static void PrepareCheckBox(CheckBox checkBox, List<IINode> nodes, string propertyName, int defaultState = 0)
        {
            checkBox.CheckState = CheckState.Indeterminate;
            foreach (var node in nodes)
            {
                if (PrepareCheckBox(checkBox, node, propertyName, defaultState))
                {
                    break;
                }
            }
        }

        public static void UpdateCheckBox(CheckBox checkBox, IINode node, string propertyName)
        {
            if (checkBox.CheckState != CheckState.Indeterminate)
            {
#if MAX2015
                node.SetUserPropBool(propertyName, checkBox.CheckState == CheckState.Checked);
#else
                node.SetUserPropBool(ref propertyName, checkBox.CheckState == CheckState.Checked);
#endif
            }
        }

        public static void UpdateCheckBox(CheckBox checkBox, List<IINode> nodes, string propertyName)
        {
            foreach (var node in nodes)
            {
                UpdateCheckBox(checkBox, node, propertyName);
            }
        }

        public static void PrepareNumericUpDown(NumericUpDown nup, List<IINode> nodes, string propertyName, float defaultState = 0)
        {
            nup.Value = (decimal)nodes[0].GetFloatProperty(propertyName, defaultState);
        }

        public static void UpdateNumericUpDown(NumericUpDown nup, List<IINode> nodes, string propertyName)
        {
            foreach (var node in nodes)
            {
#if MAX2015
                node.SetUserPropFloat(propertyName, (float)nup.Value);
#else
                node.SetUserPropFloat(ref propertyName, (float)nup.Value);
#endif
            }
        }

        public static void PrepareVector3Control(Vector3Control vector3Control, IINode node, string propertyName, float defaultX = 0, float defaultY = 0, float defaultZ = 0)
        {
            vector3Control.X = node.GetFloatProperty(propertyName + "_x", defaultX);
            vector3Control.Y = node.GetFloatProperty(propertyName + "_y", defaultY);
            vector3Control.Z = node.GetFloatProperty(propertyName + "_z", defaultZ);
        }

        public static void UpdateVector3Control(Vector3Control vector3Control, IINode node, string propertyName)
        {
            string name = propertyName + "_x";
#if MAX2015
            node.SetUserPropFloat(name, vector3Control.X);
#else
            node.SetUserPropFloat(ref name, vector3Control.X);
#endif

            name = propertyName + "_y";
#if MAX2015
            node.SetUserPropFloat(name, vector3Control.Y);
#else
            node.SetUserPropFloat(ref name, vector3Control.Y);
#endif

            name = propertyName + "_z";
#if MAX2015
            node.SetUserPropFloat(name, vector3Control.Z);
#else
            node.SetUserPropFloat(ref name, vector3Control.Z);
#endif
        }

        public static void UpdateVector3Control(Vector3Control vector3Control, List<IINode> nodes, string propertyName)
        {
            foreach (var node in nodes)
            {
                UpdateVector3Control(vector3Control, node, propertyName);
            }
        }
    }
}

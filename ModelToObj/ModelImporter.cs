using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JeremyAnsel.Media.WavefrontObj;

namespace ModelToObj
{
    public class ModelImporter
    {
        private static FileStream file;
        private static string assetPath = "D:/Steam/SteamApps/common/PAYDAY 2/assets/extract/";
        private static byte[] buffer;

        private const int animationDataTag                      = 1572868536;
        private const int authorTag                             = 1982055525;
        private const int materialGroupTag                      = 690449181;
        private const int materialTag                           = 1012162716;
        private const int object3DTag                           = 268226816;
        private const int modelDataTag                          = 1646341512;
        private const int geometryTag                           = 2058384083;
        private const int topologyTag                           = 1280342547;
        private const uint passthroughTag                       = 3819155914;
        private const int topologyIPTag                         = 62272701;

        public ObjFile Import(string modelPath)
        {
            try
            {
                buffer = File.ReadAllBytes(modelPath);
            }
            catch
            {
                Console.WriteLine("Could not find model: " + modelPath);
                return null;
            }

            int[][] sections = ParseFile();
            Dictionary<int, object[]> parsedSections = new Dictionary<int, object[]>();
        
            foreach (int[] section in sections)
            {
                switch (section[1])
                {
                    case animationDataTag:
                        //Debug.Log("animation data");
                        parsedSections[section[2]] = ParseAnimationData(section[0] + 12, section[3], section[2]);
                        break;
                    case authorTag:
                        //Debug.Log("author tag");
                        parsedSections[section[2]] = ParseAuthor(section[0] + 12, section[3], section[2]);
                        break;
                    case materialGroupTag:
                        //Debug.Log("material group tag");
                        parsedSections[section[2]] = ParseMaterialGroup(section[0] + 12, section[3], section[2]);
                        break;
                    case materialTag:
                        //Debug.Log("material tag");
                        parsedSections[section[2]] = ParseMaterial(section[0] + 12, section[3], section[2]);
                        break;
                    case object3DTag:
                        //Debug.Log("object3d tag");
                        parsedSections[section[2]] = ParseObject3D(section[0] + 12, section[3], section[2]);
                        break;
                    case modelDataTag:
                        //Debug.Log("model data tag");
                        parsedSections[section[2]] = ParseModelData(section[0] + 12, section[3], section[2]);
                        break;
                    case geometryTag:
                        //Debug.Log("geometry tag");
                        parsedSections[section[2]] = ParseGeometry(section[0] + 12, section[3], section[2]);
                        break;
                    case topologyTag:
                        //Debug.Log("topology tag");
                        parsedSections[section[2]] = ParseTopology(section[0] + 12, section[3], section[2]);
                        break;
                    case topologyIPTag:
                        //Debug.Log("topologyIP tag");
                        parsedSections[section[2]] = ParseTopologyIP(section[0] + 12, section[3], section[2]);
                        break;
                }
                if ((uint)section[1] == passthroughTag)
                {
                    //Debug.Log("passthrough tag");
                    parsedSections[section[2]] = ParsePassthroughGP(section[0] + 12, section[3], section[2]);
                }
            }

            ObjFile obj = new ObjFile();
            
            foreach (int[] section in sections)
            {
                if (section[1] == modelDataTag)
                {
                    object[] modelData = parsedSections[section[2]];
                    if ((int)modelData[3] == 6)
                        continue;
                    string modelId = "model-" + ((object[])modelData[2])[1];
                    object[] geometry = parsedSections[(int)parsedSections[(int)modelData[4]][2]];
                    object[] topology = parsedSections[(int)parsedSections[(int)modelData[4]][3]];
                    List<short[]> faces = (List<short[]>)topology[4];
                    List<float[]> verts = (List<float[]>)geometry[6];
                    List<float[]> uvs = (List<float[]>)geometry[7];
                    List<float[]> normals = (List<float[]>)geometry[8];
                    float[] position = (float[])((object[])modelData[2])[5];

                    foreach (short[] face in faces)
                    {
                        ObjFace objFace = new ObjFace();
                        objFace.Vertices.Add(new ObjTriplet(face[0] + 1, face[0] + 1, face[0] + 1));
                        objFace.Vertices.Add(new ObjTriplet(face[1] + 1, face[1] + 1, face[1] + 1));
                        objFace.Vertices.Add(new ObjTriplet(face[2] + 1, face[2] + 1, face[2] + 1));
                        obj.Faces.Add(objFace);
                    }
                    foreach (float[] vert in verts)
                    {
                        obj.Vertices.Add(new ObjVertex(vert[0] + position[0], vert[1] + position[1], vert[2] + position[2]));
                    }
                    foreach (float[] uv in uvs)
                    {
                        obj.TextureVertices.Add(new ObjVector3(uv[0], -uv[1], 0));
                    }
                    foreach (float[] normal in normals)
                    {
                        obj.VertexNormals.Add(new ObjVector3(normal[0], normal[1], normal[2]));
                    }
                }
            }

            return obj;

        }

        static int[][] ParseFile()
        {
            int sectionCount = BitConverter.ToInt32(buffer, 0);
            //int fileSize = 0;
            int currentOffset = 4;

            if (sectionCount == -1)
            {
                //fileSize = BitConverter.ToInt32(buffer, 4);
                sectionCount = BitConverter.ToInt32(buffer, 8);
                currentOffset += 8;
                //Debug.Log("Filesize: " + fileSize + " Sectioncount: " + sectionCount);
            }

            int[][] outSections = new int[sectionCount][];

            for (int i = 0; i < sectionCount; i++)
            {
                int[] pieces = ParseSectionHeader(currentOffset);
                //Debug.Log("current offset: " + currentOffset + " p0: " + pieces[0] + " p1: " + pieces[1] + " p2: " + pieces[2]);
                outSections[i] = new int[4] { currentOffset, pieces[0], pieces[1], pieces[2] };
                currentOffset += pieces[2] + 12;
            }

            return outSections;
        }

        static int[] ParseSectionHeader(int offset)
        {
            int[] pieces = new int[3];
            pieces[0] = BitConverter.ToInt32(buffer, offset);
            pieces[1] = BitConverter.ToInt32(buffer, offset + 4);
            pieces[2] = BitConverter.ToInt32(buffer, offset + 8);
            return pieces;
        }

        static string ReadString(int offset, out int newOffset)
        {
            string readString = "";
            while(buffer[offset] != 0)
            {
                readString += System.Text.Encoding.Default.GetString(new byte[1] { buffer[offset] });
                offset += 1;
            }
            newOffset = offset + 1;
            return readString;
        }

        static object[] ParseAnimationData(int offset, int size, int sectionID)
        {

            ulong unknown1 = BitConverter.ToUInt64(buffer, offset);
            int unknown2 = BitConverter.ToInt32(buffer, offset + 8);
            int unknown3 = BitConverter.ToInt32(buffer, offset + 12);
            int count = BitConverter.ToInt32(buffer, offset + 16);
            float[] items = new float[count];
            for(int i = 0; i < count; i++)
            {
                items[i] = BitConverter.ToSingle(buffer, offset + 20 + (i * 4));
            }
            return new object[7] { "Animation Data" , sectionID, unknown1, unknown2, unknown3, count, items };
        }

        static object[] ParseAuthor(int offset, int size, int sectionID)
        {
            long unknown = BitConverter.ToInt64(buffer, offset);
            int newOffset;
            string email = ReadString(offset + 8, out newOffset);
            string sourceFile = ReadString(newOffset, out newOffset);
            int unknown2 = BitConverter.ToInt32(buffer, newOffset);
            return new object[6] { "Author", sectionID, unknown, email, sourceFile, unknown2};
        }

        static object[] ParseMaterialGroup(int offset, int size, int sectionID)
        {
            int count = BitConverter.ToInt32(buffer, offset);
            int[] items = new int[count];
            for(int i = 0; i < count; i++)
            {
                items[i] = BitConverter.ToInt32(buffer, offset + 4 + (i * 4));
            }
            return new object[4] { "Material Group", sectionID, count, items };
        }

        static object[] ParseMaterial(int offset, int size, int sectionID)
        {
            return new object[2] { "Material", sectionID };
        }

        static object[] ParseObject3D(int offset, int size, int sectionID)
        {
            ulong unknown1 = BitConverter.ToUInt64(buffer, offset);
            int count = BitConverter.ToInt32(buffer, offset + 8);
            offset += 12;
            int[] items = new int[count * 3];
            for(int i = 0; i < count * 3; i++)
            {
                items[i] = BitConverter.ToInt32(buffer, offset);
                offset += 4;
            }
            float[] rotationMatrix = new float[16];
            for(int i = 0; i < rotationMatrix.Length; i++)
            {
                rotationMatrix[i] = BitConverter.ToSingle(buffer, offset);
                offset += 4;
            }
            float[] position = new float[3];
            for (int i = 0; i < position.Length; i++)
            {
                position[i] = BitConverter.ToSingle(buffer, offset);
                offset += 4;
            }
            int unknown4 = BitConverter.ToInt32(buffer, offset);
            return new object[8] { "Object3D", sectionID, unknown1, count, items, rotationMatrix, position, unknown4 };
        }

        static object[] ParseGeometry(int offset, int size, int sectionID)
        {
            int[] sizeIndex = { 0, 4, 8, 12, 16, 4, 4, 8, 12 };
            int count1 = BitConverter.ToInt32(buffer, offset);
            int count2 = BitConverter.ToInt32(buffer, offset + 4);
            offset += 8;
            List<int[]> headers = new List<int[]>();
            int calcSize = 0;
            for(int i = 0; i < count2; i++)
            {
                int itemSize = BitConverter.ToInt32(buffer, offset);
                int itemType = BitConverter.ToInt32(buffer, offset + 4);
                calcSize += sizeIndex[itemSize];
                headers.Add(new int[] { itemSize, itemType });
                offset += 8;
            }

            List<float[]> verts = new List<float[]>();
            List<float[]> uvs = new List<float[]>();
            List<float[]> normals = new List<float[]>();
            foreach (int[] header in headers)
            {
                switch (header[1])
                {
                    case 1:
                        for (int j = 0; j < count1; j++)
                        {
                            float vert0 = BitConverter.ToSingle(buffer, offset);
                            float vert1 = BitConverter.ToSingle(buffer, offset + 4);
                            float vert2 = BitConverter.ToSingle(buffer, offset + 8);
                            verts.Add(new float[] { vert0, vert1, vert2 });
                            offset += 12;
                        }
                        break;
                    case 7:
                        for (int j = 0; j < count1; j++)
                        {
                            float u = BitConverter.ToSingle(buffer, offset);
                            float v = BitConverter.ToSingle(buffer, offset + 4);
                            uvs.Add(new float[] { u, v });
                            offset += 8;
                        }
                        break;
                    case 2:
                        for (int j = 0; j < count1; j++)
                        {
                            float normal0 = BitConverter.ToSingle(buffer, offset);
                            float normal1 = BitConverter.ToSingle(buffer, offset + 4);
                            float normal2 = BitConverter.ToSingle(buffer, offset + 8);
                            offset += 12;
                            normals.Add(new float[] { normal0, normal1, normal2 });
                        }
                        break;
                    default:
                        offset += sizeIndex[header[0]] * count1;
                        break;
                }
            }

            return new object[9] { "Geometry", sectionID, count1, count2, headers, count1*calcSize, verts, uvs, normals };
        }

        static object[] ParseModelData(int offset, int size, int sectionID)
        {
            ulong unknown1 = BitConverter.ToUInt64(buffer, offset);
            int count = BitConverter.ToInt32(buffer, offset + 8);
            offset += 12;
            List<int[]> items = new List<int[]>();
            for (int i = 0; i < count; i++)
            {
                int item0 = BitConverter.ToInt32(buffer, offset);
                int item1 = BitConverter.ToInt32(buffer, offset + 4);
                int item2 = BitConverter.ToInt32(buffer, offset + 8);
                items.Add(new int[] { item0, item1, item2 });
                offset += 12;
            }

            float[] rotationMatrix = new float[16];
            for (int i = 0; i < rotationMatrix.Length; i++)
            {
                rotationMatrix[i] = BitConverter.ToSingle(buffer, offset);
                offset += 4;
            }
            float[] position = new float[3];
            for (int i = 0; i < position.Length; i++)
            {
                position[i] = BitConverter.ToSingle(buffer, offset);
                offset += 4;
            }
            int unknown4 = BitConverter.ToInt32(buffer, offset);
            offset += 4;
            object[] object3d = new object[] { "Object3D", unknown1, count, items, rotationMatrix, position, unknown4 };
            int version = BitConverter.ToInt32(buffer, offset);
            offset += 4;
            if (version == 6)
            {
                float[] unknown5 = new float[] { BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8) };
                offset += 12;
                float[] unknown6 = new float[] { BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8) };
                offset += 12;
                int unknown7 = BitConverter.ToInt32(buffer, offset);
                int unknown8 = BitConverter.ToInt32(buffer, offset + 4);
                return new object[8] { "Model", sectionID, object3d, version, unknown5, unknown6, unknown7, unknown8 };
            }
            else {
                int a = BitConverter.ToInt32(buffer, offset);
                int b = BitConverter.ToInt32(buffer, offset + 4);
                int count2 = BitConverter.ToInt32(buffer, offset + 8);
                List<int[]> items2 = new List<int[]>();
                for(int i = 0; i < count2; i++)
                {
                    items2.Add(new int[4] { BitConverter.ToInt32(buffer, offset), BitConverter.ToInt32(buffer, offset + 4), BitConverter.ToInt32(buffer, offset + 8), BitConverter.ToInt32(buffer, offset + 12) });
                    offset += 16;
                }
                return new object[8] { "Model", sectionID, object3d, version, a, b, count2, items2 };
            }
        }
        static object[] ParseTopology(int offset, int size, int sectionID)
        {
            int origOffset = offset;
            int unknown = BitConverter.ToInt32(buffer, offset);
            int count1 = BitConverter.ToInt32(buffer, offset + 4);
            offset += 8;
            List<short[]> facelist = new List<short[]>();
            for(int i = 0; i < count1 / 3; i++)
            {
                facelist.Add(new short[] { BitConverter.ToInt16(buffer, offset), BitConverter.ToInt16(buffer, offset + 2), BitConverter.ToInt16(buffer, offset + 4) });
                offset += 6;
            }
            int count2 = BitConverter.ToInt32(buffer, origOffset + 8 + count1 * 2);
            char[] items = new char[count2];
            for(int i = 0; i < count2; i++)
            {
                items[i] = BitConverter.ToChar(buffer, origOffset + 8 + count1 * 2 + 4 + i);
            }
            long unknown2 = BitConverter.ToInt64(buffer, origOffset + 8 + count1 * 2 + 4 + count2);
            return new object[8] { "Topology", sectionID, unknown, count1, facelist, count2, items, unknown2 };
        }
        static object[] ParsePassthroughGP(int offset, int size, int sectionID)
        {
            int geometrySection = BitConverter.ToInt32(buffer, offset);
            int facelistSection = BitConverter.ToInt32(buffer, offset + 4);
            return new object[] { "PassthroughGP", sectionID, geometrySection, facelistSection };
        }
        static object[] ParseTopologyIP(int offset, int size, int sectionID)
        {
            int topologySectionID = BitConverter.ToInt32(buffer, offset);
            return new object[] { "TopologyIP", sectionID, topologySectionID };
        }
    }
}

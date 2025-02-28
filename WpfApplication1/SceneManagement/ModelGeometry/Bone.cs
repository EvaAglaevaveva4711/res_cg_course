using System.Collections.Generic;
using System.Windows.Media.Media3D;


namespace WpfApplication1
{
    public class Bone
    {
        public string Name { get; set; } 
        public List<BoneWeight> ConnectedVertices { get; } = new List<BoneWeight>();
        public Matrix3D  Transform { get; set; } = Matrix3D.Identity;

        public Bone(string name)
        {
            Name = name;
        }

        public void AddVertex(int vertexID, float weight)
        {
            ConnectedVertices.Add(new BoneWeight(vertexID, Name, weight));
        }
    }
}
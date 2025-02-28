namespace WpfApplication1
{
    public class BoneWeight
    {
        public int VertexID { get; set; }
        public string BoneName { get; set; }
        public float Weight { get; set; }

        public BoneWeight(int vertexID, string boneName, float weight)
        {
            VertexID = vertexID;
            BoneName = boneName;
            Weight = weight;
        }
    }
}
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Assimp;
using Vector3D = System.Windows.Media.Media3D.Vector3D; 

namespace WpfApplication1
{
    public static class MeshGeometryCreator
    {
        public static MeshGeometry3D CreateMeshGeometry(Mesh mesh)
        {
            var geometry = new MeshGeometry3D
            {
                Positions = new Point3DCollection(mesh.Vertices.Select(v => new Point3D(v.X, v.Y, v.Z))),
                TriangleIndices = new Int32Collection(mesh.Faces.Where(f => f.Indices.Count == 3)
                    .SelectMany(f => new[] { f.Indices[0], f.Indices[2], f.Indices[1] }))
            };

            if (mesh.HasNormals)
            {
                geometry.Normals = new Vector3DCollection(mesh.Normals.Select(n => new Vector3D(-n.X, -n.Y, -n.Z)));
            }

            if (mesh.HasTextureCoords(0))
            {
                geometry.TextureCoordinates = new PointCollection(mesh.TextureCoordinateChannels[0]
                    .Select(uv => new System.Windows.Point(uv.X, 1 - uv.Y)));
            }

            return geometry;
        }
    }
}
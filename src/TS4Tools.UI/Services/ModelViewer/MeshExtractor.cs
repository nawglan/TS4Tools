// Source: legacy_references/Sims4Tools/s4pe Helpers/ModelViewer/MainWindow.xaml.cs lines 257-385 (InitScene: MLOD meshes via
//         VBUF/IBUF/VRTF chunk references, else the first chunk read as GEOM)
// Source: legacy_references/Sims4Tools/s4pe Helpers/ModelViewer/MainWindow.xaml.cs lines 440-456 (DrawModel)
// Source: legacy_references/Sims4Tools/s4pi Wrappers/GenericRCOLResource/GenericRCOLResource.cs lines 589-602 (ChunkReference.GetBlock)
// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/VRTF.cs lines 42-45 (CreateDefaultForMesh)
// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/IBUF.cs lines 62-86 (GetIndices)

using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;

namespace TS4Tools.UI.Services.ModelViewer;

/// <summary>
/// A triangle mesh ready for display: positions (x, y, z triples), optional normals and triangle indices.
/// </summary>
/// <param name="Name">Display name (legacy: "0x{name:X8}" for MLOD meshes, "GEOM Mesh" for GEOM).</param>
/// <param name="Positions">Vertex positions, 3 floats per vertex.</param>
/// <param name="Normals">Vertex normals, 3 floats per vertex, or null.</param>
/// <param name="Indices">Triangle list indices into the vertices (3 per triangle, all in range).</param>
public sealed record MeshData(string Name, float[] Positions, float[]? Normals, int[] Indices)
{
    /// <summary>Number of vertices.</summary>
    public int VertexCount => Positions.Length / 3;

    /// <summary>Number of triangles.</summary>
    public int TriangleCount => Indices.Length / 3;
}

/// <summary>The meshes found in a model resource plus the totals legacy shows in its status bar.</summary>
public sealed record ModelData(IReadOnlyList<MeshData> Meshes, int VertexCount, int PolygonCount, IReadOnlyList<string> Warnings);

/// <summary>
/// Extracts displayable meshes from MLOD (MODL LOD) and GEOM RCOL resources.
/// </summary>
/// <remarks>
/// Faithful to legacy ModelViewer.InitScene: for an RCOL containing an MLOD chunk, each mesh's vertex buffer, index buffer
/// and vertex format are resolved through public/private chunk references (external references are not supported, as in
/// legacy GetBlock); a missing VRTF uses the legacy default (sun shadow layout for shadow casters, drop shadow
/// otherwise). Indices come from <c>IBUF.GetIndices(mesh)</c> and index the mesh's own vertices. Otherwise the first
/// chunk is a GEOM whose vertex positions/normals and faces are used. Unlike legacy, a mesh that fails to load is
/// reported as a warning (legacy shows a message box) and out-of-range triangles are dropped.
/// </remarks>
public static class MeshExtractor
{
    /// <summary>
    /// Extracts the meshes of an RCOL model resource.
    /// </summary>
    public static ModelData Extract(RcolResource rcol)
    {
        ArgumentNullException.ThrowIfNull(rcol);

        var mlod = rcol.Chunks.Select(c => c.Block).OfType<MlodBlock>().FirstOrDefault();
        if (mlod != null)
            return ExtractMlod(rcol, mlod);

        var geom = rcol.Chunks.Select(c => c.Block).OfType<GeomBlock>().FirstOrDefault();
        if (geom != null)
        {
            var mesh = FromGeom(geom);
            return new ModelData([mesh], geom.VertexCount, geom.FaceCount, []);
        }

        return new ModelData([], 0, 0, ["The resource contains no MLOD or GEOM chunk."]);
    }

    /// <summary>Whether <paramref name="rcol"/> contains a mesh the viewer can show.</summary>
    public static bool HasModel(RcolResource rcol) =>
        rcol.Chunks.Any(c => c.Block is MlodBlock or GeomBlock);

    private static ModelData ExtractMlod(RcolResource rcol, MlodBlock mlod)
    {
        var meshes = new List<MeshData>();
        var warnings = new List<string>();
        int vertCount = 0, polyCount = 0;

        foreach (var m in mlod.Meshes)
        {
            // Source: MainWindow.xaml.cs lines 272-273 (counts include meshes that fail to load)
            vertCount += m.VertexCount;
            polyCount += m.PrimitiveCount;
            try
            {
                var vbuf = GetBlock(rcol, m.VertexBufferIndex) as VbufBlock
                    ?? throw new InvalidDataException("vertex buffer not found");
                var ibuf = GetBlock(rcol, m.IndexBufferIndex) as IbufBlock
                    ?? throw new InvalidDataException("index buffer not found");
                var vrtf = GetBlock(rcol, m.VertexFormatIndex) as VrtfBlock
                    ?? (m.IsShadowCaster ? VrtfBlock.CreateDefaultForSunShadow() : VrtfBlock.CreateDefaultForDropShadow());

                if (m.PrimitiveType != ModelPrimitiveType.TriangleList)
                {
                    warnings.Add($"Mesh 0x{m.Name:X8}: primitive type {m.PrimitiveType} is not drawn.");
                    continue;
                }

                var vertices = vbuf.GetVertices(m, vrtf, null);
                var indices = ibuf.GetIndices(m.PrimitiveType, m.StartIndex, m.PrimitiveCount);
                meshes.Add(FromVertices($"0x{m.Name:X8}", vertices, indices));
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentException or IndexOutOfRangeException or InvalidOperationException or NotSupportedException or OverflowException)
            {
                // Source: MainWindow.xaml.cs lines 328-331 ("Unable to load mesh id 0x{0:X8}")
                warnings.Add($"Unable to load mesh id 0x{m.Name:X8}: {ex.Message}");
            }
        }

        return new ModelData(meshes, vertCount, polyCount, warnings);
    }

    /// <summary>
    /// Resolves a chunk reference to a block of the same RCOL (public or private); external references return null.
    /// </summary>
    /// <remarks>Source: GenericRCOLResource.cs lines 589-602 (ChunkReference.GetBlock).</remarks>
    public static IRcolBlock? GetBlock(RcolResource rcol, RcolChunkReference reference)
    {
        ArgumentNullException.ThrowIfNull(rcol);
        if (reference.IsNull) return null;

        int index = reference.RefType switch
        {
            RcolReferenceType.Public => reference.Index,
            RcolReferenceType.Private => reference.Index + rcol.PublicChunksCount,
            _ => -1,
        };
        return index >= 0 && index < rcol.ChunkCount ? rcol.Chunks[index].Block : null;
    }

    /// <summary>
    /// Builds a mesh from decoded VBUF vertices and triangle-list indices.
    /// </summary>
    /// <remarks>Source: MainWindow.xaml.cs lines 440-456 (DrawModel adds positions, normals and indices as-is).</remarks>
    public static MeshData FromVertices(string name, IReadOnlyList<VbufVertex> vertices, IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);

        var positions = new float[vertices.Count * 3];
        float[]? normals = vertices.Count > 0 && vertices.All(v => v.Normal is { Length: >= 3 }) ? new float[vertices.Count * 3] : null;
        for (int i = 0; i < vertices.Count; i++)
        {
            var p = vertices[i].Position;
            if (p is { Length: >= 3 })
            {
                positions[i * 3] = p[0];
                positions[i * 3 + 1] = p[1];
                positions[i * 3 + 2] = p[2];
            }
            if (normals != null)
            {
                var n = vertices[i].Normal!;
                normals[i * 3] = n[0];
                normals[i * 3 + 1] = n[1];
                normals[i * 3 + 2] = n[2];
            }
        }

        return new MeshData(name, positions, normals, ValidTriangles(indices, vertices.Count));
    }

    /// <summary>
    /// Builds a mesh from a GEOM block's vertex positions/normals and faces.
    /// </summary>
    /// <remarks>Source: MainWindow.xaml.cs lines 337-378 (GEOM branch of InitScene).</remarks>
    public static MeshData FromGeom(GeomBlock geom)
    {
        ArgumentNullException.ThrowIfNull(geom);

        int count = geom.Vertices?.Count ?? 0;
        var positions = new float[count * 3];
        var normals = new float[count * 3];
        bool allNormals = count > 0;
        for (int i = 0; i < count; i++)
        {
            var elements = geom.Vertices![i].Elements;
            if (elements.OfType<PositionElement>().FirstOrDefault() is { } pos)
            {
                positions[i * 3] = pos.X;
                positions[i * 3 + 1] = pos.Y;
                positions[i * 3 + 2] = pos.Z;
            }
            if (elements.OfType<NormalElement>().FirstOrDefault() is { } norm)
            {
                normals[i * 3] = norm.X;
                normals[i * 3 + 1] = norm.Y;
                normals[i * 3 + 2] = norm.Z;
            }
            else
            {
                allNormals = false;
            }
        }

        var indices = new List<int>((geom.Faces?.Count ?? 0) * 3);
        if (geom.Faces != null)
        {
            foreach (var face in geom.Faces.Faces)
            {
                indices.Add(face.VertexIndex0);
                indices.Add(face.VertexIndex1);
                indices.Add(face.VertexIndex2);
            }
        }

        return new MeshData("GEOM Mesh", positions, allNormals ? normals : null, ValidTriangles(indices, count));
    }

    private static int[] ValidTriangles(IReadOnlyList<int> indices, int vertexCount)
    {
        var result = new List<int>(indices.Count - indices.Count % 3);
        for (int i = 0; i + 2 < indices.Count; i += 3)
        {
            int a = indices[i], b = indices[i + 1], c = indices[i + 2];
            if ((uint)a < (uint)vertexCount && (uint)b < (uint)vertexCount && (uint)c < (uint)vertexCount)
            {
                result.Add(a);
                result.Add(b);
                result.Add(c);
            }
        }
        return [.. result];
    }
}

// Source: legacy_references/Sims4Tools/s4pe Helpers/ModelViewer/MainWindow.xaml.cs (WPF Viewport3D scene: solid and
//         wireframe display of the meshes, camera orbit/zoom)
// Source: legacy_references/Sims4Tools/s4pe Helpers/ModelViewer/MainWindow.xaml lines 1-80 (Solid/Wireframe/Both modes)

namespace TS4Tools.UI.Services.ModelViewer;

/// <summary>How meshes are drawn.</summary>
public enum ModelRenderMode
{
    /// <summary>Flat-shaded triangles.</summary>
    Solid,
    /// <summary>Triangle edges only.</summary>
    Wireframe,
    /// <summary>Shaded triangles with edges on top.</summary>
    Both,
}

/// <summary>Orbit camera parameters.</summary>
/// <param name="Yaw">Rotation around the vertical axis, radians.</param>
/// <param name="Pitch">Rotation around the horizontal axis, radians.</param>
/// <param name="Zoom">Scale factor; 1 fits the model's bounding sphere in the view.</param>
public readonly record struct OrbitCamera(float Yaw, float Pitch, float Zoom)
{
    /// <summary>Default view: slightly from above and to the side, like legacy's initial camera.</summary>
    public static OrbitCamera Default => new(0.6f, -0.35f, 1f);
}

/// <summary>
/// Software rasteriser for the cross-platform model viewer: projects meshes orthographically into a BGRA buffer with a
/// depth buffer, flat (two-sided Lambert) shading and optional wireframe.
/// </summary>
/// <remarks>
/// Legacy uses WPF's hardware 3D (Viewport3D/HelixToolkit), which has no cross-platform equivalent in Avalonia without
/// extra packages, so a small CPU renderer draws into a bitmap instead. Vertex positions are used as stored (the game's
/// Y-up space); legacy swaps to WPF's Z-up with <c>(x, -z, y)</c>, which only changes the camera convention.
/// </remarks>
public static class SoftwareRenderer
{
    private static readonly Vec3 LightDirection = Vec3.Normalize(new Vec3(0.4f, 0.6f, 1f));

    /// <summary>
    /// Renders <paramref name="meshes"/> into a new <c>width * height * 4</c> BGRA (premultiplied, opaque) buffer.
    /// </summary>
    public static byte[] Render(IReadOnlyList<MeshData> meshes, int width, int height, OrbitCamera camera, ModelRenderMode mode)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if ((long)width * height > 64L * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(width), "Render target too large.");

        var pixels = new byte[width * height * 4];
        Clear(pixels, 0xFF2B2B30);
        var depth = new float[width * height];
        Array.Fill(depth, float.NegativeInfinity);

        var (center, radius) = GetBounds(meshes);
        if (radius <= 0) radius = 1;

        float cosYaw = MathF.Cos(camera.Yaw), sinYaw = MathF.Sin(camera.Yaw);
        float cosPitch = MathF.Cos(camera.Pitch), sinPitch = MathF.Sin(camera.Pitch);
        float scale = Math.Max(0.01f, camera.Zoom) * Math.Min(width, height) * 0.45f / radius;
        float cx = width / 2f, cy = height / 2f;

        foreach (var mesh in meshes)
        {
            int n = mesh.VertexCount;
            var screen = new Vec3[n];
            var view = new Vec3[n];
            for (int i = 0; i < n; i++)
            {
                var p = new Vec3(mesh.Positions[i * 3], mesh.Positions[i * 3 + 1], mesh.Positions[i * 3 + 2]) - center;
                // Yaw around Y, then pitch around X.
                float x1 = p.X * cosYaw + p.Z * sinYaw;
                float z1 = -p.X * sinYaw + p.Z * cosYaw;
                var v = new Vec3(x1, p.Y * cosPitch - z1 * sinPitch, p.Y * sinPitch + z1 * cosPitch);
                view[i] = v;
                screen[i] = new Vec3(cx + v.X * scale, cy - v.Y * scale, v.Z);
            }

            for (int t = 0; t + 2 < mesh.Indices.Length; t += 3)
            {
                int a = mesh.Indices[t], b = mesh.Indices[t + 1], c = mesh.Indices[t + 2];
                if (mode != ModelRenderMode.Wireframe)
                {
                    var normal = Vec3.Cross(view[b] - view[a], view[c] - view[a]);
                    float len = normal.Length();
                    float shade = len > 0 ? Math.Abs(Vec3.Dot(normal * (1f / len), LightDirection)) : 0f;
                    byte level = (byte)(40 + 200 * shade);
                    uint colour = 0xFF000000u | ((uint)(level * 0.80f) << 16) | ((uint)(level * 0.86f) << 8) | level;
                    FillTriangle(pixels, depth, width, height, screen[a], screen[b], screen[c], colour);
                }
            }

            if (mode != ModelRenderMode.Solid)
            {
                uint lineColour = mode == ModelRenderMode.Both ? 0xFF10141Cu : 0xFF9FD0FFu;
                for (int t = 0; t + 2 < mesh.Indices.Length; t += 3)
                {
                    int a = mesh.Indices[t], b = mesh.Indices[t + 1], c = mesh.Indices[t + 2];
                    DrawLine(pixels, depth, width, height, screen[a], screen[b], lineColour, mode == ModelRenderMode.Both);
                    DrawLine(pixels, depth, width, height, screen[b], screen[c], lineColour, mode == ModelRenderMode.Both);
                    DrawLine(pixels, depth, width, height, screen[c], screen[a], lineColour, mode == ModelRenderMode.Both);
                }
            }
        }

        return pixels;
    }

    /// <summary>Bounding-sphere centre and radius of all vertices.</summary>
    internal static (Vec3 Center, float Radius) GetBounds(IReadOnlyList<MeshData> meshes)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        var min = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vec3(float.MinValue, float.MinValue, float.MinValue);
        bool any = false;
        foreach (var mesh in meshes)
        {
            for (int i = 0; i + 2 < mesh.Positions.Length; i += 3)
            {
                var p = new Vec3(mesh.Positions[i], mesh.Positions[i + 1], mesh.Positions[i + 2]);
                if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z)) continue;
                min = new Vec3(Math.Min(min.X, p.X), Math.Min(min.Y, p.Y), Math.Min(min.Z, p.Z));
                max = new Vec3(Math.Max(max.X, p.X), Math.Max(max.Y, p.Y), Math.Max(max.Z, p.Z));
                any = true;
            }
        }
        if (!any) return (default, 0f);
        var center = (min + max) * 0.5f;
        return (center, (max - min).Length() / 2f);
    }

    private static void Clear(byte[] pixels, uint bgra)
    {
        var span = MemoryMarshal.Cast<byte, uint>(pixels.AsSpan());
        span.Fill(bgra);
    }

    private static void SetPixel(byte[] pixels, int index, uint bgra)
    {
        int o = index * 4;
        pixels[o] = (byte)bgra;
        pixels[o + 1] = (byte)(bgra >> 8);
        pixels[o + 2] = (byte)(bgra >> 16);
        pixels[o + 3] = (byte)(bgra >> 24);
    }

    private static void FillTriangle(byte[] pixels, float[] depth, int width, int height, Vec3 a, Vec3 b, Vec3 c, uint colour)
    {
        if (!IsFinite(a) || !IsFinite(b) || !IsFinite(c)) return;

        float area = Edge(a, b, c.X, c.Y);
        if (Math.Abs(area) < 1e-6f) return;

        int minX = Math.Max(0, (int)MathF.Floor(Math.Min(a.X, Math.Min(b.X, c.X))));
        int maxX = Math.Min(width - 1, (int)MathF.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))));
        int minY = Math.Max(0, (int)MathF.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y))));
        int maxY = Math.Min(height - 1, (int)MathF.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y))));
        if (minX > maxX || minY > maxY) return;

        for (int y = minY; y <= maxY; y++)
        {
            float py = y + 0.5f;
            for (int x = minX; x <= maxX; x++)
            {
                float px = x + 0.5f;
                float w0 = Edge(b, c, px, py) / area;
                float w1 = Edge(c, a, px, py) / area;
                float w2 = 1f - w0 - w1;
                if (w0 < 0 || w1 < 0 || w2 < 0) continue;

                float z = w0 * a.Z + w1 * b.Z + w2 * c.Z;
                int index = y * width + x;
                if (z <= depth[index]) continue;
                depth[index] = z;
                SetPixel(pixels, index, colour);
            }
        }
    }

    private static void DrawLine(byte[] pixels, float[] depth, int width, int height, Vec3 a, Vec3 b, uint colour, bool depthTest)
    {
        if (!IsFinite(a) || !IsFinite(b)) return;
        float dx = b.X - a.X, dy = b.Y - a.Y;
        int steps = (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)));
        if (steps > 4 * (width + height)) return; // off-screen garbage
        steps = Math.Max(steps, 1);
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = (int)(a.X + dx * t);
            int y = (int)(a.Y + dy * t);
            if ((uint)x >= (uint)width || (uint)y >= (uint)height) continue;
            int index = y * width + x;
            float z = a.Z + (b.Z - a.Z) * t;
            // Small bias so edges of visible faces win against their own face.
            if (depthTest && z + 1e-3f * Math.Max(1f, Math.Abs(z)) < depth[index]) continue;
            SetPixel(pixels, index, colour);
        }
    }

    private static float Edge(Vec3 a, Vec3 b, float x, float y) => (b.X - a.X) * (y - a.Y) - (b.Y - a.Y) * (x - a.X);

    private static bool IsFinite(Vec3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}

/// <summary>Minimal 3D vector for the software renderer.</summary>
internal readonly record struct Vec3(float X, float Y, float Z)
{
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vec3 operator *(Vec3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);

    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vec3 Cross(Vec3 a, Vec3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    public static Vec3 Normalize(Vec3 v)
    {
        var len = v.Length();
        return len > 0 ? v * (1f / len) : v;
    }
}

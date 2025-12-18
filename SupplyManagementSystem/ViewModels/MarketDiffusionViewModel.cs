using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SupplyManagementSystem.Data;
using SupplyManagementSystem.Models;

namespace SupplyManagementSystem.ViewModels;

public partial class MarketDiffusionViewModel : ObservableObject
{
    #region Constants

    private const int Width = 200;
    private const int Height = 200;

    private const double DiffusionRate = 0.28;
    private const double CompetitionFactor = 0.1;
    private const int SeedSize = 5;

    private const double MinStrength = 0.95;
    private const double MaxStrength = 1.05;

    private const double MinBrightness = 0.3;

    #endregion

    #region Fields

    [ObservableProperty]
    private WriteableBitmap? _bitmap;

    private double[,,] _field = null!;
    private double[,,] _next = null!;
    private byte[,] _colors = null!;
    private double[] _supplierStrength = null!;

    private readonly Random _random = new();
    private DispatcherTimer? _timer;
    private List<Supplier> _suppliers = null!;

    private readonly double[,] _kernel =
    {
        { 0,   0.7, 0 },
        { 0.7, -2.8, 0.7 },
        { 0,   0.7, 0 }
    };

    #endregion

    #region Commands

    public IRelayCommand RestartCommand => new RelayCommand(Restart);

    #endregion

    public MarketDiffusionViewModel()
    {
        _ = InitDiffusion();
    }

    #region Initialization

    private async Task InitDiffusion()
    {
        await using var db = new AppDbContext();
        _suppliers = await db.Suppliers
            .AsNoTracking()
            .Take(10)
            .ToListAsync();

        if (_suppliers.Count == 0)
            return;

        InitializeArrays();
        CalculateSupplierStrength();
        GenerateColors();
        InitializeBitmap();
        PlaceRandomSeeds();
    }

    private void InitializeArrays()
    {
        int count = _suppliers.Count;
        _field = new double[count, Width, Height];
        _next = new double[count, Width, Height];
        _colors = new byte[count, 3];
    }

    private void CalculateSupplierStrength()
    {
        // Предварительные максимумы для нормализации
        double maxCost = _suppliers.Max(s => (double)s.SupplyCost);
        double maxSpeed = _suppliers.Max(s => s.DeliverySpeedDays);

        // 1. Считаем "сырую" силу
        double[] rawStrength = _suppliers.Select(s =>
        {
            double quality     = s.Quality / 100.0;
            double reliability = s.Reliability / 100.0;
            double experience  = Math.Min(s.ExperienceYears / 10.0, 1.0);

            double costFactor  = 1.0 - Math.Min((double)s.SupplyCost / maxCost, 1.0);
            double speedFactor = 1.0 - Math.Min(s.DeliverySpeedDays / maxSpeed, 1.0);

            return
                0.30 * quality +
                0.25 * reliability +
                0.15 * experience +
                0.15 * costFactor +
                0.15 * speedFactor;
        }).ToArray();

        // 2. Нормализация в диапазон [0.95; 1.05]
        double minRaw = rawStrength.Min();
        double maxRaw = rawStrength.Max();

        _supplierStrength = rawStrength
            .Select(v =>
                MinStrength +
                (v - minRaw) / (maxRaw - minRaw + 1e-6) *
                (MaxStrength - MinStrength)
            )
            .ToArray();
    }

    private void GenerateColors()
    {
        for (int i = 0; i < _suppliers.Count; i++)
        {
            _colors[i, 0] = RandomColor();
            _colors[i, 1] = RandomColor();
            _colors[i, 2] = RandomColor();
        }
    }

    private byte RandomColor() => (byte)_random.Next(50, 256);

    private void InitializeBitmap()
    {
        Bitmap = new WriteableBitmap(
            new PixelSize(Width, Height),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Opaque);
    }

    private void PlaceRandomSeeds()
    {
        for (int i = 0; i < _suppliers.Count; i++)
        {
            AddSupplierSeed(
                i,
                _random.Next(Width),
                _random.Next(Height),
                SeedSize
            );
        }
    }

    #endregion

    #region Control

    public void Start()
    {
        if (_timer != null) return;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    public void Restart()
    {
        Stop();
        ClearField();
        PlaceRandomSeeds();
        Start();
    }

    private void ClearField()
    {
        Array.Clear(_field);
    }

    #endregion

    #region Simulation

    private void Tick()
    {
        StepDiffusion();
        ApplyCompetition();
        RenderBitmap();
    }

    private void StepDiffusion()
    {
        int count = _suppliers.Count;

        for (int s = 0; s < count; s++)
        for (int x = 1; x < Width - 1; x++)
        for (int y = 1; y < Height - 1; y++)
        {
            double sum = 0;

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                sum += _field[s, x + dx, y + dy] * _kernel[dx + 1, dy + 1];

            _next[s, x, y] =
                Math.Max(0, _field[s, x, y] + DiffusionRate * sum);
        }

        (_field, _next) = (_next, _field);
    }

    private void ApplyCompetition()
    {
        int count = _suppliers.Count;

        for (int x = 1; x < Width - 1; x++)
        for (int y = 1; y < Height - 1; y++)
        {
            double total = 0;

            for (int s = 0; s < count; s++)
                total += (_field[s, x, y] *= _supplierStrength[s]);

            if (total < 0.001)
            {
                for (int s = 0; s < count; s++)
                    _field[s, x, y] = 0;
                continue;
            }

            for (int s1 = 0; s1 < count; s1++)
            for (int s2 = 0; s2 < count; s2++)
            {
                if (s1 == s2) continue;

                double diff = _field[s1, x, y] - _field[s2, x, y];
                if (diff > 0)
                    _field[s2, x, y] =
                        Math.Max(0, _field[s2, x, y] - CompetitionFactor * diff);
            }
        }
    }

    #endregion

    #region Rendering

    private unsafe void RenderBitmap()
    {
        if (Bitmap == null) return;

        using var fb = Bitmap.Lock();
        byte* buffer = (byte*)fb.Address;
        int stride = fb.RowBytes;

        int count = _suppliers.Count;

        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            double r = 0, g = 0, b = 0;

            for (int s = 0; s < count; s++)
            {
                double v = _field[s, x, y];
                r += (_colors[s, 0] / 255.0) * v;
                g += (_colors[s, 1] / 255.0) * v;
                b += (_colors[s, 2] / 255.0) * v;
            }

            NormalizeColor(ref r, ref g, ref b);

            int i = y * stride + x * 4;
            buffer[i + 0] = (byte)(b * 255);
            buffer[i + 1] = (byte)(g * 255);
            buffer[i + 2] = (byte)(r * 255);
            buffer[i + 3] = 255;
        }
    }

    private static void NormalizeColor(ref double r, ref double g, ref double b)
    {
        double max = Math.Max(r, Math.Max(g, b));
        if (max > 1.0)
        {
            double scale = 1.0 / max;
            r *= scale;
            g *= scale;
            b *= scale;
        }

        r = r * (1 - MinBrightness) + MinBrightness;
        g = g * (1 - MinBrightness) + MinBrightness;
        b = b * (1 - MinBrightness) + MinBrightness;
    }

    #endregion

    #region Helpers

    private void AddSupplierSeed(int index, int cx, int cy, int size)
    {
        int half = size / 2;

        for (int dx = -half; dx <= half; dx++)
        for (int dy = -half; dy <= half; dy++)
        {
            int x = cx + dx;
            int y = cy + dy;

            if (x is >= 0 and < Width && y is >= 0 and < Height)
                _field[index, x, y] = 1.0;
        }
    }

    #endregion
}

using System;
using System.Threading;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SupplyManagementSystem.ViewModels
{
    public partial class MarketDiffusionViewModel : ObservableObject
    {
        private const int Width = 200;
        private const int Height = 200;
        private const int SupplierCount = 3;
        private double[,,] _field; // [supplier, x, y]
        private byte[,,] _colors;  // предрасчет цветов
        private Random _rnd = new();

        [ObservableProperty]
        private WriteableBitmap? _bitmap;

        public MarketDiffusionViewModel()
        {
            _field = new double[SupplierCount, Width, Height];
            _colors = new byte[SupplierCount, 3, 1]; // RGB

            // Инициализация цветов
            _colors[0, 0, 0] = 255; _colors[0, 1, 0] = 0; _colors[0, 2, 0] = 0;     // красный
            _colors[1, 0, 0] = 0; _colors[1, 1, 0] = 0; _colors[1, 2, 0] = 255;     // синий
            _colors[2, 0, 0] = 0; _colors[2, 1, 0] = 255; _colors[2, 2, 0] = 0;     // зеленый

            // Инициализация точек
            for (int s = 0; s < SupplierCount; s++)
            {
                int x0 = _rnd.Next(Width / 4, 3 * Width / 4);
                int y0 = _rnd.Next(Height / 4, 3 * Height / 4);
                _field[s, x0, y0] = 1.0;
            }

            // Создаем WriteableBitmap
            Bitmap = new WriteableBitmap(new PixelSize(Width, Height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Opaque);

            // Запуск таймера
            var timer = new Timer(_ => Dispatcher.UIThread.Post(() => Tick()), null, 0, 50);
        }

        private void Tick()
        {
            StepDiffusion();
            RenderBitmap();
        }

        private void StepDiffusion()
        {
            var next = new double[SupplierCount, Width, Height];
            double spread = 0.25;

            for (int s = 0; s < SupplierCount; s++)
            {
                for (int x = 1; x < Width - 1; x++)
                {
                    for (int y = 1; y < Height - 1; y++)
                    {
                        double val = _field[s, x, y];
                        double d = val * spread;
                        next[s, x, y] += val - 4 * d;
                        next[s, x + 1, y] += d;
                        next[s, x - 1, y] += d;
                        next[s, x, y + 1] += d;
                        next[s, x, y - 1] += d;
                    }
                }
            }

            _field = next;
        }

        private unsafe void RenderBitmap()
        {
            using var fb = Bitmap!.Lock();
            int stride = fb.RowBytes;

            byte* buffer = (byte*)fb.Address;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float r = 0, g = 0, b = 0;

                    for (int s = 0; s < SupplierCount; s++)
                    {
                        double val = _field[s, x, y];
                        r += (float)(_colors[s, 0, 0] / 255.0 * val);
                        g += (float)(_colors[s, 1, 0] / 255.0 * val);
                        b += (float)(_colors[s, 2, 0] / 255.0 * val);
                    }

                    r = Math.Min(r, 1f);
                    g = Math.Min(g, 1f);
                    b = Math.Min(b, 1f);

                    int index = y * stride + x * 4;
                    buffer[index + 0] = (byte)(b * 255);
                    buffer[index + 1] = (byte)(g * 255);
                    buffer[index + 2] = (byte)(r * 255);
                    buffer[index + 3] = 255; // alpha
                }
            }
        }
    }
}

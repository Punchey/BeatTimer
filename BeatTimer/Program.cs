﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using FFTWSharp;

namespace BeatTimer
{
    class BeatTimer
    {
        static void Main(string[] args)
        {
            double[] data;
            WAV.readWav(args[0], out data);
            var spec = spectrogram(data, 2048, 128);
            Console.WriteLine(spec.Length);
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(spec[i]);
            }
        }

        /// <summary>
        ///   Calculates the spectral flux of a real double array
        ///   <example>
        ///     <code>
        ///       var specflux = spectrogram(somedata, 4096, 1024);
        ///     </code>
        ///   </example>
        /// </summary>
        /// <param name="arr">Any real data</param>
        /// <param name="size">FFT length (power of 2)</param>
        /// <param name="step">How much to increment for start of following FFT</param>
        /// <returns>Array reprenting change in sum of frequencies over time</returns>
        static double[] spectrogram(double[] arr, int size, int step)
        {
            int n = arr.Length;
            // FFT of real values is symmetric, so we only need an array half the length + 1
            int fftlength = size / 2 + 1;
            // The number of groupings of size shifted step apart you can fit in n, minus 10
            int speclength = (n - size + step) / step - 10;

            double[] spec = new double[speclength];
            double[,] ffts = new double[11,fftlength];
            // FFT is returned as complex number with every other index being imaginary, so double length
            double[] fft = new double[fftlength * 2];

            IntPtr ptr = fftw.malloc(size * sizeof(double));
            IntPtr ptrout = fftw.malloc(fftlength * 2 * sizeof(double));
            IntPtr plan = fftw.dft_r2c_1d(size, ptr, ptrout, fftw_flags.Estimate);

            for (int i = 0; i + size + step < n; i += step)
            {
                int index = i / step;
                Marshal.Copy(arr, i, ptr, size);
                fftw.execute(plan);
                Marshal.Copy(ptrout, fft, 0, fftlength * 2);
                for (int j = 0; j < fftlength * 2; j += 2)
                {
                    // Set to absolute value of complex number
                    ffts[index % 11, j / 2] = Math.Sqrt(fft[j] * fft[j] + fft[j + 1] * fft[j + 1]);
                }
                if (index >= 10)
                {
                    for (int j = 0; j < fftlength; j++)
                    {
                        double diff = ffts[index % 11, j] - ffts[(index - 10) % 11, j];
                        // Only add difference if positive. Results in better results
                        spec[index - 10] += diff > 0 ? diff : 0;
                    }
                }
            }
            fftw.destroy_plan(plan);
            fftw.free(ptr);
            fftw.cleanup();
            return spec;
        }
    }

    // https://stackoverflow.com/questions/8754111/how-to-read-the-data-in-a-wav-file-to-an-array
    class WAV
    {
        static double bytesToDouble(byte firstByte, byte secondByte)
        {
            short s = (short)((secondByte << 8) | firstByte);
            return s / 32768.0;
        }

        public static void readWav(string filename, out double[] audio)
        {
            byte[] wav = File.ReadAllBytes(filename);
            int channels = wav[22];
            int pos = 12;
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;

            int samples = (wav.Length - pos) / 2;
            if (channels == 2) samples /= 2;

            audio = new double[samples];

            int i = 0;
            while (pos < wav.Length)
            {
                audio[i] = bytesToDouble(wav[pos], wav[pos + 1]);
                pos += 2;
                if (channels == 2)
                {
                    audio[i] += bytesToDouble(wav[pos], wav[pos + 1]);
                    pos += 2;
                }
                i++;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Numerics;

namespace Fractals
{
	class Program
	{
		static double realMin = -2;
		static double realMax = 2;
		static double imagMin = -2;
		static double imagMax = 2;
		static int imgWidth = 640;
		static int imgHeight = 640;
		static string fileName = "zad18.png";
		static int maxIter = 700;

		static int threadCount = 1;
		static int[][] blocks;
		static int blockIndex;
		static ManualResetEvent[] doneEvents;

		static bool isQuiet = false;

		static int blockCount;


		static void Main(string[] args)
		{
			
			for(int i = 0; i < args.Length; i+=2)
			{
				if(args[i] == "-q" || args[i] == "-quiet")
				{
					isQuiet = true;
				}
				else if(args[i] == "-s" || args[i] == "-size")
				{
					SetSize(args[i + 1]);
				}
				else if(args[i] == "-r" || args[i] == "-rect")
				{
					SetRect(args[i + 1]);
				}
				else if(args[i] == "-t" || args[i] == "-tasks")
				{
					SetThreadNum(args[i + 1]);
				}
				else if(args[i] == "-o" || args[i] == "-output")
				{
					fileName = args[i + 1];
				}
			}

			Bitmap img = new Bitmap(imgWidth, imgHeight);

			Console.WriteLine("Threads used in current run: {0}", threadCount);
			DateTime currentTime = DateTime.Now;
			

			if(!isQuiet)
			{
				Console.WriteLine("Main thread id: {0}", Thread.CurrentThread.ManagedThreadId);
			}

			if(threadCount == 1)
			{
				fractal(img, 0, 0, imgWidth, imgHeight);
			}
			else
			{
				doneEvents = new ManualResetEvent[threadCount - 1];
				blockIndex = 0;
					
				CalcBlocks();
				Thread[] threads = new Thread[threadCount - 1];
				for(int i = 0; i < threadCount - 1; i++)
				{
					doneEvents[i] = new ManualResetEvent(false);
					threads[i] = new Thread((x) =>
					{
						drawFractalBlock(img, (int)x);
					});
					threads[i].Start(i);
				}

				while(blockIndex < blockCount)
				{
					int[] block;

					block = blocks[blockIndex++];

					fractal(img, block[0], block[1], block[2], block[3]);
				}

				WaitHandle.WaitAll(doneEvents);
				
				for(int i = 0; i < threads.Length; i++)
				{
					threads[i].Join();
				}
			}

			TimeSpan executionTime = DateTime.Now - currentTime;
			float milliseconds = (executionTime.Hours * 3600 + executionTime.Minutes * 60 + executionTime.Seconds) * 1000 + executionTime.Milliseconds;

			
			Console.WriteLine("Total execution time {0}ms", milliseconds);

			img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
		}

		static void SetThreadNum(string threadNum)
		{
			int num;
			if(!int.TryParse(threadNum, out num))
			{
				return;
			}

			threadCount = num;
		}

		static void SetSize(string size)
		{
			string[] dims = size.Split('x');
			
			if(dims.Length != 2)
			{
				return;
			}

			int width, height;
			if(!int.TryParse(dims[0], out width) || !int.TryParse(dims[1], out height))
			{
				return;
			}

			imgWidth = width;
			imgHeight = height;
		}

		static void SetRect(string rect)
		{
			string[] extents = rect.Split(':');
			
			if(extents.Length == 4)
			{
				double rMin, rMax, iMin, iMax;
				if(!double.TryParse(extents[0], out rMin) || !double.TryParse(extents[1], out rMax)
					|| !double.TryParse(extents[2], out iMin) || !double.TryParse(extents[3], out iMax))
				{
					return;
				}

				realMin = rMin;
				realMax = rMax;
				imagMin = iMin;
				imagMax = iMax;
			}

		}

		static void CalcBlocks()
		{
			int granularity = threadCount;
			int blockWidth = imgWidth / granularity;
			int blockHeight = imgHeight / granularity;

			int columnNum = granularity;
			int rowNum = granularity;

			if(imgWidth % granularity != 0)
			{
				columnNum = granularity + 1;
			}

			if(imgHeight % granularity != 0)
			{
				rowNum = granularity + 1;
			}

			blocks = new int[rowNum * columnNum][];

			for(int i = 0; i < granularity; i++)
			{
				for(int j = 0; j < granularity; j++)
				{
					blocks[i * granularity + j] = new int[] { i * blockWidth, j * blockHeight, blockWidth, blockHeight };
				}
			}

			if(rowNum > granularity)
			{
				for(int i = 0; i < granularity; i++)
				{
					blocks[granularity * granularity + i] = new int[] { i * blockWidth, granularity * blockHeight, blockWidth, imgHeight % granularity };
				}

				blocks[granularity * granularity + granularity] = new int[] { granularity * blockWidth, granularity * blockHeight, imgWidth % granularity, imgHeight % granularity };
			}

			if(columnNum > granularity)
			{

				for(int i = 0; i < granularity; i++)
				{
					blocks[granularity * granularity + columnNum + i] = new int[] { granularity * blockWidth, i * blockHeight, imgWidth % granularity, blockHeight };
				}

			}

			blockCount = blocks.Length;
		}

		static void drawFractalBlock(Bitmap img, int index)
		{
			if(!isQuiet)
			Console.WriteLine("Thread {0} started", Thread.CurrentThread.ManagedThreadId);
		
			DateTime now = DateTime.Now;
			try
			{
				while(blockIndex < blockCount)
				{
					int[] block;
					
					block = blocks[blockIndex++];
					
					fractal(img, block[0], block[1], block[2], block[3]);
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("Error in thread {0}: {1}\n{2}", Thread.CurrentThread.ManagedThreadId, e.Message, e.StackTrace);
			}
			finally
			{
				doneEvents[index].Set();
			}

			TimeSpan runingTime = DateTime.Now.Subtract(now);


			if(!isQuiet)
			{
				float milliseconds = (runingTime.Hours * 3600 + runingTime.Minutes * 60 + runingTime.Seconds) * 1000 + runingTime.Milliseconds;
				Console.WriteLine("Thread {0} stopped", Thread.CurrentThread.ManagedThreadId);
				Console.WriteLine("Thread {0} execution time {1}ms", Thread.CurrentThread.ManagedThreadId, milliseconds);
			}
		}

		static Color mandelbrot(Complex c)
		{
			Complex z = c;
			int i = 0;
			for(; i < maxIter && z.Magnitude < 50; i++)
			{
				z = Complex.Exp(Complex.Cos(Complex.Multiply(z, c)));
			}

			float k = i / maxIter;

			return Color.FromArgb((int)(255 * k),(int)( 80 * (1 - k) + 255 * k), (int)(140 * (1 - k) + 255 * k));
		}

		static void fractal(Bitmap img, int startX, int startY, int width, int height)
		{
			int w = imgWidth;
			int h = imgHeight;
			double rMin = realMin;
			double rMax = realMax;
			double iMin = imagMin;
			double iMax = imagMax;

			for(int x = startX; x < startX + width; x++)
			{
				for(int y = startY; y < startY + height; y++)
				{
					double r = (rMax - rMin) * x / (w - 1) + rMin;
					double i = (iMax - iMin) * y / (h - 1) + iMin;

					Color pixelColor = mandelbrot(new Complex(r, i));

					lock(img)
					{
						try
						{
							img.SetPixel(x, y, pixelColor);
						}
						catch(Exception e)
						{
							Console.WriteLine("{0} {1} {2} {3}", startX, startY, width, height);
							return;
						}
					}
				}

			}
		}

	}
}

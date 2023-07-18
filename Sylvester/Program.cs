using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using BitMiracle.LibTiff.Classic;


namespace Sylvester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Sylvester MultiPurpose Converter");
                Console.WriteLine("Syntax");
                Console.WriteLine("Sylvester.exe <string> <y/n> <y/n>");
                Console.WriteLine("Sylvester.exe <folder-path> <multi-only> <subdirectories>");
                Console.WriteLine("Default is to convert ALL files within ALL subdirectories.");

            }
            else
            {
                if ((args[0] != null) && (args[0] == "downgrade"))
                {
                    if ((args[1] != null) && (Directory.Exists(args[1])))
                    {
                        string[] FileList;

                        //uggh this if ladder tho
                        if (args.Length > 3)
                        {
                            if (args[3] == "n")  //no subdirectories
                            {
                                if (args[2] == "y")  //only multi files
                                {
                                    FileList = Directory.GetFiles(args[1], "*multi*.tif", SearchOption.TopDirectoryOnly);
                                }
                                else
                                {
                                    FileList = Directory.GetFiles(args[1], "*.tif", SearchOption.TopDirectoryOnly);
                                }


                            }
                            else
                            {
                                if (args[2] == "y")  //only multi files
                                {
                                    FileList = Directory.GetFiles(args[1], "*multi*.tif", SearchOption.AllDirectories);
                                }
                                else
                                {
                                    FileList = Directory.GetFiles(args[1], "*.tif", SearchOption.AllDirectories);
                                }
                            }

                        }
                        else
                        {
                            if (args.Length > 2)
                            {
                                if (args[2] == "y")  //only multi files
                                {
                                    FileList = Directory.GetFiles(args[1], "*multi*.tif", SearchOption.AllDirectories);
                                }
                                else
                                {
                                    FileList = Directory.GetFiles(args[1], "*.tif", SearchOption.AllDirectories);
                                }
                            }
                            else
                            {
                                FileList = Directory.GetFiles(args[1], "*.tif", SearchOption.AllDirectories);
                            }
                        }


                        int FileCount = FileList.Length;
                        for (int ThisFile = 0; ThisFile < FileCount; ThisFile++)
                        {
                            if ((FileList[ThisFile] != null) && (File.Exists(FileList[ThisFile])))
                            {
                                FileStream DataStream = new FileStream(FileList[ThisFile], FileMode.Open);
                                Bitmap SourceData = (Bitmap)Image.FromStream(DataStream);
                                DataStream.Close();

                                using (Tiff tiff = Tiff.Open(FileList[ThisFile], "w"))
                                {


                                    int TargetWidth = SourceData.Width;
                                    int TargetHeight = SourceData.Height;


                                    //lol AI go brr
                                    tiff.SetField(TiffTag.IMAGEWIDTH, TargetWidth);
                                    tiff.SetField(TiffTag.IMAGELENGTH, TargetHeight);
                                    tiff.SetField(TiffTag.SAMPLESPERPIXEL, 4); // RGBA
                                    tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
                                    tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                                    tiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
                                    tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);



                                    for (int Height = 0; Height < SourceData.Height; Height++)
                                    {
                                        byte[] ColorData = new byte[SourceData.Width * 4];

                                        int ByteIndex = 0;

                                        for (int Width = 0; Width < SourceData.Width; Width++)
                                        {
                                            /*
                                                Red > blue
                                                Green: no change
                                                blue > alpha
                                                Alpha > red
                                            */


                                            Color ThisColor = SourceData.GetPixel(Width, Height);
                                            Color NewColor = Color.FromArgb
                                                (
                                                    ThisColor.B,
                                                    ThisColor.A,
                                                    ThisColor.G,
                                                    ThisColor.R
                                                );

                                            if (SourceData.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                                            {
                                                ColorData[ByteIndex * 4] = NewColor.R;   // Red
                                                ColorData[ByteIndex * 4 + 1] = NewColor.G; // Green
                                                ColorData[ByteIndex * 4 + 2] = NewColor.B;     // Blue
                                                ColorData[ByteIndex * 4 + 3] = NewColor.A; // Alpha
                                            }
                                            else
                                            {
                                                ColorData[ByteIndex * 4] = 0;   // no alpha in source
                                                ColorData[ByteIndex * 4 + 1] = NewColor.G; // Green
                                                ColorData[ByteIndex * 4 + 2] = NewColor.B;     // Blue
                                                ColorData[ByteIndex * 4 + 3] = NewColor.A; // Alpha
                                            }

                                            ByteIndex++;
                                        }
                                        tiff.WriteScanline(ColorData, Height);
                                    }

                                    // Create a separate alpha channel
                                    if (SourceData.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                                    {


                                        tiff.WriteDirectory(); // Create a new TIFF directory

                                        // Set TIFF tags for the alpha channel
                                        tiff.SetField(TiffTag.IMAGEWIDTH, TargetWidth);
                                        tiff.SetField(TiffTag.IMAGELENGTH, TargetHeight);
                                        tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1); // Alpha
                                        tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
                                        tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);

                                        // Write alpha channel data
                                        for (int Height = 0; Height < SourceData.Height; Height++)
                                        {
                                            byte[] alphaData = new byte[TargetWidth];

                                            int ByteIndex = 0;
                                            for (int Width = 0; Width < SourceData.Width; Width++)
                                            {

                                                Color ThisColor = SourceData.GetPixel(Width, Height);
                                                Color NewColor = Color.FromArgb
                                                    (
                                                        ThisColor.B,
                                                        ThisColor.A,
                                                        ThisColor.G,
                                                        ThisColor.R
                                                    );

                                                alphaData[Width] = NewColor.A;
                                                ByteIndex++;
                                            }
                                            tiff.WriteScanline(alphaData, Height);
                                        }
                                    }

                                    // Save and close the TIFF file
                                    tiff.Close();
                                }

                            }
                        }
                    }
                }
                else
                {
                    if ((args[0] != null) && (args[0] == "upgrade"))
                    {
                        if ((args[1] != null) && (Directory.Exists(args[1])))
                        {
                            string[] FileList;
                            //uggh this if ladder tho
                            if (args.Length > 3)
                            {
                                if (args[3] == "n")  //no subdirectories
                                {
                                    if (args[2] == "y")  //only multi files
                                    {
                                        FileList = Directory.GetFiles(args[1], "*multi*.tif", SearchOption.TopDirectoryOnly);
                                    }
                                    else
                                    {
                                        FileList = Directory.GetFiles(args[1], "*.tif", SearchOption.TopDirectoryOnly);
                                    }


                                }
                                else
                                {
                                    if (args[2] == "y")  //only multi files
                                    {
                                        FileList = Directory.GetFiles(args[1], "*multi*.tif", SearchOption.AllDirectories);
                                    }
                                    else
                                    {
                                        FileList = Directory.GetFiles(args[1], "*.tif", SearchOption.AllDirectories);
                                    }
                                }

                            }
                            else
                            {
                                if (args.Length > 2)
                                {
                                    if (args[2] == "y")  //only multi files
                                    {
                                        FileList = Directory.GetFiles(args[1], "*multi*.tif", SearchOption.AllDirectories);
                                    }
                                    else
                                    {
                                        FileList = Directory.GetFiles(args[1], "*.tif", SearchOption.AllDirectories);
                                    }
                                }
                                else
                                {
                                    FileList = Directory.GetFiles(args[1], "*.tif", SearchOption.AllDirectories);
                                }
                            }

                            int FileCount = FileList.Length;
                            for (int ThisFile = 0; ThisFile < FileCount; ThisFile++)
                            {
                                if ((FileList[ThisFile] != null) && (File.Exists(FileList[ThisFile])))
                                {
                                    FileStream DataStream = new FileStream(FileList[ThisFile], FileMode.Open);
                                    Bitmap SourceData = (Bitmap)Image.FromStream(DataStream);
                                    DataStream.Close();

                                    using (Tiff tiff = Tiff.Open(FileList[ThisFile], "w"))
                                    {


                                        int TargetWidth = SourceData.Width;
                                        int TargetHeight = SourceData.Height;


                                        //lol AI go brr
                                        tiff.SetField(TiffTag.IMAGEWIDTH, TargetWidth);
                                        tiff.SetField(TiffTag.IMAGELENGTH, TargetHeight);
                                        tiff.SetField(TiffTag.SAMPLESPERPIXEL, 4); // RGBA
                                        tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
                                        tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                                        tiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
                                        tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);



                                        for (int Height = 0; Height < SourceData.Height; Height++)
                                        {
                                            byte[] ColorData = new byte[SourceData.Width * 4];

                                            int ByteIndex = 0;

                                            for (int Width = 0; Width < SourceData.Width; Width++)
                                            {
                                                /*
                                                    Red > blue
                                                    Green: no change
                                                    blue > alpha
                                                    Alpha > red
                                                */


                                                Color ThisColor = SourceData.GetPixel(Width, Height);
                                                Color NewColor = Color.FromArgb
                                                    (
                                                        ThisColor.R,
                                                        ThisColor.B,
                                                        ThisColor.G,
                                                        ThisColor.A
                                                    );

                                                ColorData[ByteIndex * 4] = NewColor.R;   // Red
                                                ColorData[ByteIndex * 4 + 1] = NewColor.G; // Green
                                                ColorData[ByteIndex * 4 + 2] = NewColor.B;     // Blue
                                                ColorData[ByteIndex * 4 + 3] = NewColor.A; // Alpha

                                                ByteIndex++;
                                            }
                                            tiff.WriteScanline(ColorData, Height);
                                        }

                                        // Create a separate alpha channel
                                        tiff.WriteDirectory(); // Create a new TIFF directory

                                        // Set TIFF tags for the alpha channel
                                        tiff.SetField(TiffTag.IMAGEWIDTH, TargetWidth);
                                        tiff.SetField(TiffTag.IMAGELENGTH, TargetHeight);
                                        tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1); // Alpha
                                        tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
                                        tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);

                                        // Write alpha channel data
                                        for (int Height = 0; Height < SourceData.Height; Height++)
                                        {
                                            byte[] alphaData = new byte[TargetWidth];

                                            int ByteIndex = 0;
                                            for (int Width = 0; Width < SourceData.Width; Width++)
                                            {

                                                /*
                                                    Red > blue
                                                    Green: no change
                                                    blue > alpha
                                                    Alpha > red
                                                */


                                                Color ThisColor = SourceData.GetPixel(Width, Height);
                                                Color NewColor = Color.FromArgb
                                                    (
                                                        ThisColor.R,
                                                        ThisColor.B,
                                                        ThisColor.G,
                                                        ThisColor.A
                                                    );
                                                alphaData[Width] = NewColor.A;
                                                ByteIndex++;
                                            }
                                            tiff.WriteScanline(alphaData, Height);
                                        }

                                        // Save and close the TIFF file
                                        tiff.Close();
                                    }

                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Sylvester MultiPurpose Converter");
                        Console.WriteLine("Syntax");
                        Console.WriteLine("Sylvester.exe <string> <y/n> <y/n>");
                        Console.WriteLine("Sylvester.exe <folder-path> <multi-only> <subdirectories>");
                        Console.WriteLine("Default is to convert ALL files within ALL subdirectories.");
                    }
                }
            }
        }
    }
}

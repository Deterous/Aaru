using System;
using Aaru.Checksums;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Aaru.Tests.Images
{
    public abstract class TapeMediaImageTest : BaseMediaImageTest
    {
        // How many sectors to read at once
        const uint SECTORS_TO_READ = 256;

        public abstract TapeImageTestExpected[] Tests { get; }

        [Test]
        public void Tape()
        {
            Environment.CurrentDirectory = _dataFolder;

            Assert.Multiple(() =>
            {
                foreach(TapeImageTestExpected test in Tests)
                {
                    string  testFile    = test.TestFile;
                    var     filtersList = new FiltersList();
                    IFilter filter      = filtersList.GetFilter(testFile);
                    filter.Open(testFile);

                    var image = Activator.CreateInstance(_plugin.GetType()) as ITapeImage;
                    Assert.NotNull(image, $"Could not instantiate filesystem for {testFile}");

                    bool opened = image.Open(filter);
                    Assert.AreEqual(true, opened, $"Open: {testFile}");

                    if(!opened)
                        continue;

                    Assert.AreEqual(true, image.IsTape, $"Is tape?: {testFile}");

                    using(new AssertionScope())
                    {
                        Assert.Multiple(() =>
                        {
                            image.Files.Should().BeEquivalentTo(test.Files, $"Tape files: {testFile}");

                            image.TapePartitions.Should().
                                  BeEquivalentTo(test.Partitions, $"Tape partitions: {testFile}");
                        });
                    }
                }
            });
        }

        [Test]
        public void Info()
        {
            Environment.CurrentDirectory = _dataFolder;

            Assert.Multiple(() =>
            {
                foreach(TapeImageTestExpected test in Tests)
                {
                    string  testFile    = test.TestFile;
                    var     filtersList = new FiltersList();
                    IFilter filter      = filtersList.GetFilter(testFile);
                    filter.Open(testFile);

                    var image = Activator.CreateInstance(_plugin.GetType()) as IMediaImage;
                    Assert.NotNull(image, $"Could not instantiate filesystem for {testFile}");

                    bool opened = image.Open(filter);
                    Assert.AreEqual(true, opened, $"Open: {testFile}");

                    if(!opened)
                        continue;

                    using(new AssertionScope())
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.AreEqual(test.Sectors, image.Info.Sectors, $"Sectors: {testFile}");
                            Assert.AreEqual(test.SectorSize, image.Info.SectorSize, $"Sector size: {testFile}");
                            Assert.AreEqual(test.MediaType, image.Info.MediaType, $"Media type: {testFile}");
                        });
                    }
                }
            });
        }

        [Test]
        public void Hashes()
        {
            Environment.CurrentDirectory = _dataFolder;

            Assert.Multiple(() =>
            {
                foreach(TapeImageTestExpected test in Tests)
                {
                    string  testFile    = test.TestFile;
                    var     filtersList = new FiltersList();
                    IFilter filter      = filtersList.GetFilter(testFile);
                    filter.Open(testFile);

                    var image = Activator.CreateInstance(_plugin.GetType()) as IMediaImage;
                    Assert.NotNull(image, $"Could not instantiate filesystem for {testFile}");

                    bool opened = image.Open(filter);
                    Assert.AreEqual(true, opened, $"Open: {testFile}");

                    if(!opened)
                        continue;

                    ulong doneSectors = 0;
                    var   ctx         = new Md5Context();

                    while(doneSectors < image.Info.Sectors)
                    {
                        byte[] sector;

                        if(image.Info.Sectors - doneSectors >= SECTORS_TO_READ)
                        {
                            sector      =  image.ReadSectors(doneSectors, SECTORS_TO_READ);
                            doneSectors += SECTORS_TO_READ;
                        }
                        else
                        {
                            sector      =  image.ReadSectors(doneSectors, (uint)(image.Info.Sectors - doneSectors));
                            doneSectors += image.Info.Sectors - doneSectors;
                        }

                        ctx.Update(sector);
                    }

                    Assert.AreEqual(test.MD5, ctx.End(), $"Hash: {testFile}");
                }
            });
        }
    }
}
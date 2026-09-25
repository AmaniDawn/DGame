using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Pipeline;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Unit tests for MaxLengthStream - the size-limited read wrapper that caps HTTP request bodies.
    /// </summary>
    public class MaxLengthStreamTests
    {
        private static MemoryStream BytesOf(int count)
        {
            return new MemoryStream(new byte[count]);
        }

        [Test]
        public void Read_WithinLimit_ReturnsAllData()
        {
            using var inner = BytesOf(500);
            using var limited = new MaxLengthStream(inner, 1024);

            var buffer = new byte[4096];
            var total = 0;
            int read;
            while ((read = limited.Read(buffer, total, buffer.Length - total)) > 0)
            {
                total += read;
            }

            Assert.AreEqual(500, total);
        }

        [Test]
        public void Read_ExactlyAtLimit_IsAllowed()
        {
            using var inner = BytesOf(1024);
            using var limited = new MaxLengthStream(inner, 1024);

            var buffer = new byte[4096];
            var total = 0;
            int read;
            while ((read = limited.Read(buffer, total, buffer.Length - total)) > 0)
            {
                total += read;
            }

            Assert.AreEqual(1024, total, "A body exactly at the cap must be accepted.");
        }

        [Test]
        public void Read_OverLimit_Throws()
        {
            using var inner = BytesOf(2048);
            using var limited = new MaxLengthStream(inner, 1024);

            var buffer = new byte[4096];
            Assert.Throws<RequestTooLargeException>(() =>
            {
                // Single read pulls more than the cap.
                limited.Read(buffer, 0, buffer.Length);
            });
        }

        [Test]
        public async Task StreamReader_OverLimit_ThrowsWhileReading()
        {
            // Exercise the path the server actually uses: StreamReader.ReadToEndAsync over the wrapper.
            var payload = new string('x', 5000);
            using var inner = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            using var limited = new MaxLengthStream(inner, 1024);
            using var reader = new StreamReader(limited);

            try
            {
                await reader.ReadToEndAsync();
                Assert.Fail("Expected RequestTooLargeException for an over-limit body.");
            }
            catch (RequestTooLargeException ex)
            {
                Assert.AreEqual(1024, ex.MaxBytes);
            }
        }

        [Test]
        public async Task StreamReader_WithinLimit_ReadsFullBody()
        {
            var payload = new string('y', 800);
            using var inner = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            using var limited = new MaxLengthStream(inner, 1024);
            using var reader = new StreamReader(limited);

            var result = await reader.ReadToEndAsync();

            Assert.AreEqual(payload, result);
        }
    }
}

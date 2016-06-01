﻿using Xunit;

namespace Microsoft.AspNetCore.WebHooks.Utilities
{
    public class HasherTests
    {
        public static TheoryData<string, uint> HashData
        {
            get
            {
                // Reference data obtained from http://find.fnvhash.com/
                return new TheoryData<string, uint>
                {
                    { string.Empty, 0x811c9dc5 },
                    { " ", 0x250c8f7f },
                    { "\\r\\n", 0x29a7e301 },
                    { "The quick brown fox jumped over the lazy dog.", 0x47998ae2 },
                    { "😀 😁 😂 😃 😄 😅 😆 😇", 0x9c3b5c7f },
                    { "你好世界, 안녕하세요, مرحبا بالعالم , Merhaba Dünya, Здравей Свят", 0x747cfaf8 },
                };
            }
        }

        [Theory]
        [MemberData("HashData")]
        public void GetFnvHash32_ReturnsExpectedResult(string input, uint expected)
        {
            // Act
            uint actual = Hasher.GetFnvHash32(input);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}

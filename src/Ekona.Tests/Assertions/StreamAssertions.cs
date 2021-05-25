// Copyright(c) 2021 SceneGate
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System;
using System.IO;
using System.Security.Cryptography;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Yarhl.IO;

namespace SceneGate.Ekona.Tests.Assertions
{
    public class StreamAssertions :
        ReferenceTypeAssertions<Stream, StreamAssertions>
    {
        public StreamAssertions(Stream instance)
            : base(instance)
        {
        }

        protected override string Identifier => "stream";

        [CustomAssertion]
        public AndConstraint<StreamAssertions> MatchInfo(BinaryInfo info)
        {
            Subject.Should()
                .BeAssignableTo<DataStream>("We will compare its offset")
                .Which
                .Offset
                .Should().Be(info.Offset);
            Subject.Length.Should().Be(info.Length);

            if (!string.IsNullOrEmpty(info.Sha256)) {
                HaveSha256(info.Sha256);
            }

            return new AndConstraint<StreamAssertions>(this);
        }

        [CustomAssertion]
        public AndConstraint<StreamAssertions> HaveSha256(string hash)
        {
            Subject.Position = 0;
            using var sha256 = SHA256.Create();
            sha256.ComputeHash(Subject);
            string actualHash = BitConverter.ToString(sha256.Hash)
                .Replace("-", string.Empty)
                .ToLowerInvariant();

            Execute.Assertion
                .ForCondition(!string.IsNullOrEmpty(hash))
                .FailWith("hash is empty")
                .Then
                .ForCondition(hash == actualHash)
                .FailWith("Expected {context} to have SHA-256 {0} but was {1}", hash, actualHash);

            return new AndConstraint<StreamAssertions>(this);
        }
    }
}

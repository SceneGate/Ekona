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
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Yarhl.FileSystem;

namespace SceneGate.Ekona.Tests.Assertions
{
    public class NodeAssertions
            : ReferenceTypeAssertions<Node, NodeAssertions>
    {
        public NodeAssertions(Node instance)
            : base(instance)
        {
        }

        protected override string Identifier => "node";

        [CustomAssertion]
        public AndConstraint<NodeAssertions> MatchInfo(NodeContainerInfo info)
        {
            Subject.Name.Should().Be(info.Name);
            Subject.Format?.GetType().FullName.Should().Be(info.Format);

            if (info.Tags != null) {
                // YAML deserializer always gets the value as a string
                foreach (var entry in info.Tags) {
                    Subject.Tags.Should().ContainKey(entry.Key);
                    entry.Value.Should().Be(Subject.Tags[entry.Key].ToString());
                }
            }

            if (info.Stream != null) {
                Subject.Stream.Should().MatchInfo(info.Stream);
            }

            int expectedCount = info.Children?.Count ?? 0;
            if (info.CheckChildren) {
                Subject.Children.Should().HaveCount(expectedCount);
            }

            for (int i = 0; i < expectedCount; i++) {
                NodeContainerInfo expectedNode = info.Children[i];
                using (new AssertionScope(expectedNode.Name)) {
                    Subject.Children.Should().Contain(n => n.Name == expectedNode.Name);
                    Subject.Children[expectedNode.Name].Should().MatchInfo(expectedNode);
                }
            }

            return new AndConstraint<NodeAssertions>(this);
        }
    }
}

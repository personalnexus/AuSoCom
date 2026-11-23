using AuSoCom.Domain;
using FluentAssertions;

namespace AuSoComTests;

[TestClass]
public sealed class ShowTests
{
    [TestMethod]
    public void BackAndNextNavigateTheOrderedScenes()
    {
        var show = new Show();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        show.Add(first);
        show.Add(second);

        show.Start().Should().Be(first);
        show.CurrentSceneIndex.Should().Be(0);
        show.Next().Should().Be(second);
        show.CurrentSceneIndex.Should().Be(1);
        show.Back().Should().Be(first);
        show.CurrentSceneIndex.Should().Be(0);
    }

    [TestMethod]
    public void NextWrapsToTheFirstScene()
    {
        var show = new Show();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        show.Add(first);
        show.Add(second);
        show.Start();

        show.Next();
        show.Next().Should().Be(first);
        show.CurrentSceneIndex.Should().Be(0);
    }

    [TestMethod]
    public void StartCanSelectTheChosenScene()
    {
        var show = new Show();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        show.Add(first);
        show.Add(second);

        show.Start(second).Should().Be(second);
        show.CurrentSceneId.Should().Be(second);
    }

    [TestMethod]
    public void ActiveShowPositionDistinguishesRepeatedSceneEntries()
    {
        var show = new Show();
        var sceneId = Guid.NewGuid();
        show.Add(sceneId);
        show.Add(sceneId);

        show.StartAtIndex(1).Should().Be(sceneId);
        show.CurrentSceneIndex.Should().Be(1);
        show.Next();

        show.CurrentSceneIndex.Should().Be(0);
    }

    [TestMethod]
    public void RemovingTheCurrentSceneClearsCurrentSelection()
    {
        var show = new Show();
        var sceneId = Guid.NewGuid();
        show.Add(sceneId);
        show.Start();

        show.Remove(sceneId);

        show.CurrentSceneId.Should().BeNull();
        show.SceneIds.Should().BeEmpty();
    }
}

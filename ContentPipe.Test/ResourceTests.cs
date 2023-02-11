namespace ContentPipe.Test;

public class Tests
{
	[SetUp]
	public void Setup()
	{
	}

	[Test]
	public void SingleFileLoad()
	{
		ContentDirectory.CompressDirectory("Content/ResourcePass");
		Content.LoadDirectory("Content/ResourcePass");

		string resourceContent = Content.LoadString("file_passed.txt");
		if (resourceContent != "Test passed!")
		{
			Assert.Fail("Resource content was not as expected. Received: '" + resourceContent + "', expected 'Test passed!'");
		}
		Assert.Pass("Resource matched the expected content");
	}
	
	[Test]
	public void MultiFileLoad()
	{
		ContentDirectory.CompressDirectory("Content/ResourcePass");
		ContentDirectory.CompressDirectory("Content/ResourcePassMulti");
		Content.LoadDirectory("Content/ResourcePassMulti");
		Content.LoadDirectory("Content/ResourcePass");

		string resourceContent = Content.LoadString("file_passed.txt");
		if (resourceContent != "Test passed!")
		{
			Assert.Fail("Resource content was not as expected. Received: '" + resourceContent + "', expected 'Test passed!'");
		}
		Assert.Pass("Resource matched the expected content");
	}
	
	[Test]
	public void PhysicalLoadMulti()
	{
		ContentDirectory.CompressDirectory("Content/ResourcePassMulti");
		Content.LoadDirectory("Content/ResourcePassMulti");
		Content.LoadPhysicalDirectory("Content/ResourcePass");

		string resourceContent = Content.LoadString("file_passed.txt");
		if (resourceContent != "Test passed!")
		{
			Assert.Fail("Resource content was not as expected. Received: '" + resourceContent + "', expected 'Test passed!'");
		}
		Assert.Pass("Resource matched the expected content");
	}
	
	[Test]
	public void PhysicalLoad()
	{
		Content.LoadPhysicalDirectory("Content/ResourcePass");

		string resourceContent = Content.LoadString("file_passed.txt");
		if (resourceContent != "Test passed!")
		{
			Assert.Fail("Resource content was not as expected. Received: '" + resourceContent + "', expected 'Test passed!'");
		}
		Assert.Pass("Resource matched the expected content");
	}

	[TearDown]
	public void Unload()
	{
		Content.UnloadAll();
	}
}

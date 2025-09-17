namespace HttpMataki.NET.UnitTests;

public class InMemoryHttpObserverTests
{
    [Fact]
    public async Task OnHttpCommunicationAsync_AddsRecordToCollection()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var record = new HttpCommunicationRecord
        {
            Request = new HttpRequestData
            {
                Method = "GET",
                Uri = "https://example.com"
            }
        };

        // Act
        await observer.OnHttpCommunicationAsync(record);

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        Assert.Equal(record.Id, records[0].Id);
        Assert.Equal("GET", records[0].Request.Method);
        Assert.Equal("https://example.com", records[0].Request.Uri);
    }

    [Fact]
    public async Task OnHttpCommunicationAsync_MultipleRecords_AddsAllToCollection()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var record1 = new HttpCommunicationRecord { Request = new HttpRequestData { Method = "GET" } };
        var record2 = new HttpCommunicationRecord { Request = new HttpRequestData { Method = "POST" } };

        // Act
        await observer.OnHttpCommunicationAsync(record1);
        await observer.OnHttpCommunicationAsync(record2);

        // Assert
        var records = observer.GetRecords();
        Assert.Equal(2, records.Count);
        Assert.Contains(records, r => r.Request.Method == "GET");
        Assert.Contains(records, r => r.Request.Method == "POST");
    }

    [Fact]
    public void Clear_RemovesAllRecords()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var record = new HttpCommunicationRecord { Request = new HttpRequestData { Method = "GET" } };
        observer.OnHttpCommunicationAsync(record).Wait();

        // Act
        observer.Clear();

        // Assert
        var records = observer.GetRecords();
        Assert.Empty(records);
    }

    [Fact]
    public void GetRecords_ReturnsImmutableSnapshot()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var record = new HttpCommunicationRecord { Request = new HttpRequestData { Method = "GET" } };
        observer.OnHttpCommunicationAsync(record).Wait();

        // Act
        var records1 = observer.GetRecords();
        var record2 = new HttpCommunicationRecord { Request = new HttpRequestData { Method = "POST" } };
        observer.OnHttpCommunicationAsync(record2).Wait();
        var records2 = observer.GetRecords();

        // Assert
        Assert.Single(records1);
        Assert.Equal(2, records2.Count);
        Assert.Equal("GET", records1[0].Request.Method);
    }
}
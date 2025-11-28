using Amazon.S3;
using Amazon.S3.Model;

namespace SqlVersioningService.Infrastructure;

public class S3Storage
{
    private readonly IAmazonS3 _s3;

    public S3Storage(IAmazonS3 s3) => _s3 = s3;

    public async Task UploadAsync(string bucket, string key, string content)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            ContentBody = content
        };

        await _s3.PutObjectAsync(request);
    }
}

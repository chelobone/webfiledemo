namespace WebFileLoader.Interfaces
{
	public interface IAWSConfig
	{
		string AwsAccessKey { get; set; }
		string AwsSecretAccessKey { get; set; }
		string AwsSessionToken { get; set; }
		string BucketName { get; set; }
		string Region { get; set; }
	}
}

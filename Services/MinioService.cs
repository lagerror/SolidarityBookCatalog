using Minio;
using Minio.DataModel.Args;

namespace SolidarityBookCatalog.Services
{
    //minio 文件存储
    public class MinioService
    {
        public MinioClient _minioClient=new MinioClient();
        public MinioService(IConfiguration configuration) {
            _minioClient
                .WithEndpoint(configuration["Minio:Ip"].ToString())
                .WithCredentials(configuration["Minio:UserName"].ToString(), configuration["Minio:Password"].ToString());
            _minioClient.Build ();
        }
      
    }
}

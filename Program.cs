// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using Microsoft.Data.SqlClient;
using Minio;
using Minio.DataModel.Args;
using System.Text.RegularExpressions;

//Console.WriteLine("Hello, World!");

#region Definitions

const string CONFIG_FILE_NAME = @"MigratorConfig.json";
const string SQL_QUERY_FILE = @"SqlQuery.txt";

var _setting = new Setting();

var  Variables = new Dictionary<string, string> {

    { "FileName","Name"},
    { "FileFullName","FullName"},
    { "UploadFileName",""},
    { "CurrentFolder","Directory.Name"},
    { "CurrentFullDir","DirectoryName"},

};

#endregion

#region Auxilary MEthods
string FillSqlQueryBodyVariables(string text, FileInfo fInfo, string minioFileName)
{

    try
    {
        var source = text ?? string.Empty;
        string pattern = @"\{\{.*?\}\}";
        RegexOptions options = RegexOptions.Multiline;
       
        var variables = Regex.Matches(source, pattern, options);

        if (variables?.Count > 0)
        {

            for (var i = 0; i < variables.Count; i++)
            {
                var match = variables[i];

                var variable = match.ToString() ?? string.Empty;

                var replacer = new Regex(Regex.Escape(variable));


                ////var tag = tagsInTemplate[i];

                var varName = match.Groups[0].Value.Replace("{", "").Replace("}", "");
                //var varName = match.Groups[0].Value;


                if (!Variables.ContainsKey(varName)) throw new Exception($"Não possível encontrar o valor para a variavel {varName}");

                var fieldName = Variables[varName];
                dynamic value;

                //if (varName.Contains("File")) value = fInfo.GetDynRuntimeValue(fieldName, true);
                if (varName == "UploadFileName")
                    value = minioFileName;
                else value = fInfo.GetDynRuntimeValue(fieldName, true);


                if (string.IsNullOrEmpty($"{value}"))
                {
                    source = replacer.Replace(source, string.Empty);
                    continue;
                }

                source = replacer.Replace(source, value.ToString());
            }
        }



        return source;
    }
    catch (Exception ex)
    {

        throw new Exception(ex.Message);
    }
}
async Task<bool> UploadToMinIo(MemoryStream ms, string fileName)
{
    try
    {
        var client = new MinioClient()
            .WithEndpoint(_setting.MiniIoEndPoint)
            .WithCredentials(_setting.MiniIoAcessKey, _setting.MiniIoSecretKey)
            .Build();

        fileName = fileName.Replace(' ', '_');

        var args = new PutObjectArgs()
            .WithBucket(_setting.MiniIoBucket)
            .WithObject(fileName)
            .WithStreamData(ms)
            .WithObjectSize(ms.Length);

        await client.PutObjectAsync(args);
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return false;
    }
}
async Task<bool> RemoveFromMinIoFile(string FileName)
{
    try
    {
        var client = new MinioClient()
            .WithEndpoint(_setting.MiniIoEndPoint)
            .WithCredentials(_setting.MiniIoAcessKey, _setting.MiniIoSecretKey)
            .Build();

        var minIoArguments = new RemoveObjectArgs()
            .WithBucket(_setting.MiniIoBucket)
            .WithObject(FileName);

        await client.RemoveObjectAsync(minIoArguments);
        return true;
    }
    catch (Exception ex)
    {
        throw new Exception(ex.Message);
    }
}
async Task UpdateDatabaseAsync(FileInfo info, string minioFileName)
{

    var conn = new SqlConnection(_setting.SqlConnection);

    var query = await File.ReadAllTextAsync(SQL_QUERY_FILE);

    query = FillSqlQueryBodyVariables(query, info, minioFileName);


    if (!query.ToLower().Contains("where"))
    {
        throw new Exception("Lamento muito, para o seu bem estar emocional não poderei executar uma Query sem 'WHERE Clause' 😊");
    }

    await using var cmd = new SqlCommand(query, conn);

    await conn.OpenAsync();

    await cmd.ExecuteNonQueryAsync();

    await conn.CloseAsync();
    await conn.DisposeAsync();
}
void DrawTextProgressBar(int progress, int total)
{
    //draw empty progress bar
    Console.CursorLeft = 0;
    Console.Write("["); //start
    Console.CursorLeft = 32;
    Console.Write("]"); //end
    Console.CursorLeft = 1;
    float onechunk = 30.0f / total;

    //draw filled part
    int position = 1;
    for (int i = 0; i < onechunk * progress; i++)
    {
        Console.BackgroundColor = ConsoleColor.Gray;
        Console.CursorLeft = position++;
        Console.Write(" ");
    }

    //draw unfilled part
    for (int i = position; i <= 31; i++)
    {
        Console.BackgroundColor = ConsoleColor.DarkBlue;
        Console.CursorLeft = position++;
        Console.Write(" ");
    }

    //draw totals
    Console.CursorLeft = 35;
    Console.BackgroundColor = ConsoleColor.Black;
    Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
}
void DrawWelcomeText() {

    Console.WriteLine("BEM VINDO AO MIGRADOR DE FICHEIROS PARA O MINIO ");

    Console.WriteLine();
    Console.WriteLine();

    Console.WriteLine("Variaveis de Ambiente:");


    Console.WriteLine();
    Console.WriteLine();

    _setting.SqlConnection.WriteBuityfullConnectionString();
    Console.WriteLine();
  
    Console.WriteLine($"  DIRECTORIO DE ORIGEM: {_setting.RootDirectory}");
    Console.WriteLine();

    Console.WriteLine($"  MINIO ENDPOINT: {_setting.MiniIoEndPoint}");
    //Console.WriteLine($"  MINIO ACCESS KEY: {_setting.MiniIoAcessKey}");
    //Console.WriteLine($"  MINIO SECRET KEY: {_setting.MiniIoSecretKey}");
    Console.WriteLine($"  MINIO BUCKET: {_setting.MiniIoBucket}");

  
    Console.WriteLine();
    Console.WriteLine();
  
    Console.Write("Precione ENTER para  iniciar a migração");
    Console.ReadLine();

}
#endregion

#region Main Method
async Task RunMigrator()
{

    try
    {


        var jsonText = await File.ReadAllTextAsync(CONFIG_FILE_NAME);

        _setting = System.Text.Json.JsonSerializer.Deserialize<Setting>(jsonText) ?? new Setting();
                
        DrawWelcomeText();

        if (!Directory.Exists(_setting.RootDirectory))
        {
            Console.WriteLine($"Não foi possível encontrar o directório {_setting.RootDirectory}");
            return;
        }

        var folders = Directory.GetDirectories(_setting.RootDirectory);

        if (folders.Length > 0) {
           
            var op = new ParallelOptions { MaxDegreeOfParallelism = 10 };
            var count = 0;

            await Parallel.ForEachAsync(folders, op, async (folder, ct) => {

                var files = Directory.GetFiles(folder, "*.*");

                if (files.Length < 1 || files is null) return;

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    var dInfo = new DirectoryInfo(folder);

                    var ms = new MemoryStream(await File.ReadAllBytesAsync(file));
                    var minioFileName = $"itf_{Guid.NewGuid()}{Path.GetExtension(info.Name)}".Replace("-", string.Empty);
                    try
                    {
                        await UploadToMinIo(ms, minioFileName);
                        await UpdateDatabaseAsync(info, minioFileName);
                        await ms.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        await RemoveFromMinIoFile(minioFileName);
                        throw new Exception(ex.Message);
                    }
                }

                count++;
                DrawTextProgressBar(count, folders.Length);
            });

        }
     
        Console.WriteLine("Processo Concluido!");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }

}

#endregion
// starting main method
await RunMigrator();

Console.ReadLine();


using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CleanDN_CC_v2
{
    public class Program
    {
        static void Main(string[] args)
        {            
            string LogFile = ConfigurationSettings.AppSettings["LogFile"];
            string Folder = ConfigurationSettings.AppSettings["FolderToProcessFiles"];
            string FolderToBackup = ConfigurationSettings.AppSettings["FolderToBackupFiles"];
            string FolderNonProcessed = ConfigurationSettings.AppSettings["FolderNonProcessedFiles"];
            string FileToProcess = ConfigurationSettings.AppSettings["FileToProcess"];
            double MaxValue = double.Parse(ConfigurationSettings.AppSettings["MaxValue"]);
            string[] filesInFolder;
            int fileQuantity = 0;

            Log("\n################### Inicio do processamento ###################", true);
            Console.WriteLine("################### Inicio do processamento ###################");
            try
            {
                filesInFolder = Directory.GetFiles(Folder, FileToProcess + "*");  //procura os LHDIFs que tiverem na pasta
                fileQuantity = filesInFolder.Length;  //quantidade de arquivos encontrados
            }
            catch (Exception ex)
            {
                Log($"\nNenhum arquivo {FileToProcess} encontrado! Ignorando o processamento...\n\n", false);
                Console.WriteLine($"\nNenhum arquivo {FileToProcess} encontrado! Ignorando o processamento...\n\n");
                return;
            }
            string FileValidName = "";    //nome do novo arquivo corrigido
            string FileInvalidName = "";    //nome do novo arquivo COM o(s) DN(s) incorretos

            List<string> FileValid = new List<string>();
            List<string> FileInvalid = new List<string>();

            if (fileQuantity > 0)
            {
                Log($"Iniciando o processamento de {fileQuantity} arquivos.", false);
                Console.WriteLine($"Iniciando o processamento de {fileQuantity} arquivos.");
                int counter = 1;
                foreach (var file in filesInFolder) //processa arquivo por arquivo encontrado na pasta...
                {
                    Log($"Processando o arquivo {counter}/{fileQuantity}...", false);
                    Console.WriteLine($"Processando o arquivo {counter}/{fileQuantity}...");
                    try
                    {
                        string[] allLines = File.ReadAllLines(file);

                        foreach (var l in allLines) //validação de cada linha...
                        {
                            string[] DnActual = l.Split('#');
                            double value = 0;
                            double.TryParse(DnActual[5], out value);

                            if (value > MaxValue)
                            {
                                FileInvalid.Add(l);
                            }
                            else
                            {
                                FileValid.Add(l);
                            }
                        }

                        if (FileInvalid.Count > 0)  //só executa alguma ação se encontrar algum DN inválido...
                        {
                            string[] actual = file.Split('\\');
                            string actualName = actual.Last();
                            actual = actualName.Split('.');
                            actualName = $"{actual[0]}.{actual[1]}";
                            //FileWithBanName = $"{FileToProcess}.D{DateTime.Now.ToString("yyyyMMdd")}.T{DateTime.Now.ToString("HHmmss")}.txt";   //nome do arquivo com erros...
                            FileInvalidName = $"{actualName}.TXT.ERROR.D{DateTime.Now.ToString("yyyyMMdd")}.T{DateTime.Now.ToString("HHmmss")}";   //nome do arquivo com erros...

                            using (StreamWriter sw = new StreamWriter(FolderNonProcessed + FileInvalidName)) //cria o arquivo somente dos banidos...
                            {
                                foreach (string newLines in FileInvalid)
                                {
                                    sw.WriteLine(newLines);
                                }
                            }
                            Log($"Gerado o arquivo com os DNs inválidos em {FolderNonProcessed}{FileInvalidName}...", false);
                            Console.WriteLine($"Gerado o arquivo com os DNs inválidos em {FolderNonProcessed}{FileInvalidName}...");

                            Thread.Sleep(3000); //intervalo de 3s por segurança...

                            string BkpOriginalFile = $"{actualName}.TXT.ORIGINAL.D{DateTime.Now.ToString("yyyyMMdd")}.T{DateTime.Now.ToString("HHmmss")}";
                            File.Move(file, FolderToBackup + BkpOriginalFile);    //renomeia o arquivo original para não ser processado...
                            Log($"Arquivo {actualName}.TXT renomeado e movido para {FolderToBackup}{BkpOriginalFile}...", false);
                            Console.WriteLine($"Arquivo {actualName}.TXT renomeado e movido para {FolderToBackup}{BkpOriginalFile}...");

                            Thread.Sleep(3000); //intervalo de 3s por segurança...

                            //FileWithoutBanName = $"{actualName}.TXT.SB.D{DateTime.Now.ToString("yyyyMMdd")}.T{DateTime.Now.ToString("HHmmss")}";   //nome do arquivo sem ignorados...
                            FileValidName = $"{actualName}.TXT";   //nome do arquivo sem ignorados...

                            using (StreamWriter sw = new StreamWriter(Folder + FileValidName)) //cria o arquivo somente sem os banidos...
                            {
                                foreach (string newLines in FileValid)
                                {
                                    sw.WriteLine(newLines);
                                }
                            }
                            Log($"Gerado o arquivo válido em {Folder}{FileValidName}...", false);
                            Console.WriteLine($"Gerado o arquivo válido em {Folder}{FileValidName}...");
                        }
                        else
                        {
                            Log($"Nenhum DN inválido encontrado! Nenhuma alteração realizada no arquivo {file}", false);
                            Console.WriteLine($"Nenhum DN inválido encontrado! Nenhuma alteração realizada no arquivo {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Erro ao processar o arquivo {file}. {ex.Message}", false);
                        Console.WriteLine($"Erro ao processar o arquivo {file}.\n{ex.Message}");
                    }
                    counter++;
                }
            }
            else
            {
                Log($"Nenhum arquivo {FileToProcess} para processar em {Folder}", false);
                Console.WriteLine($"Nenhum arquivo {FileToProcess} para processar em {Folder}");
            }

            Log("#################### Fim do processamento #####################\n", true);
            Console.WriteLine("#################### Fim do processamento ####################\n");

            void Log(string message, bool special)
            {
                using (StreamWriter swLog = new StreamWriter(LogFile, true))
                {
                    if (special)
                    {
                        swLog.WriteLine(message);
                    }
                    else
                    {
                        swLog.WriteLine($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} => {message}");
                    }
                }
            }

        }
    }
    
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

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
            int linha = 1;
            

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
            string MailBody;

            if (fileQuantity > 0)
            {
                Log($"Iniciando o processamento de {fileQuantity} arquivos.", false);
                Console.WriteLine($"Iniciando o processamento de {fileQuantity} arquivos.");
                int counter = 1;
                foreach (var file in filesInFolder) //processa arquivo por arquivo encontrado na pasta...
                {
                    //limpa as lists antes de processar o arquivo para não duplicar os dados caso tenha mais de um arquivo...
                    FileValid.Clear();
                    FileInvalid.Clear();
                    MailBody = "";

                    //preparação do corpo do e-MailBody
                    MailBody += $"<html><head></head><body>\n<h2>\nCleanDNs 2.0 - Relatório de processamento - {DateTime.Now.ToString("dd/MM/yyyy")}\n</h2>\n<table>";

                    Log($"Processando o arquivo {counter}/{fileQuantity}...", false);
                    Console.WriteLine($"Processando o arquivo {counter}/{fileQuantity}...");
                    try
                    {
                        string[] allLines = File.ReadAllLines(file);

                        string[] procDate = allLines[0].Split('#');
                        string procDateFiltered = DateTime.Parse(procDate[1]).ToString("yyyyMMdd");
                        string timeFile = File.GetCreationTime(file).ToString("HHmmss");

                        foreach (var l in allLines) //validação de cada linha...
                        {
                            string[] DnActual = l.Split('#');
                            string doc = DnActual[5];
                            double value = 0;
                            double.TryParse(DnActual[5], out value);

                            if (value > MaxValue)
                            {
                                FileInvalid.Add(l);
                                MailBody += $"\n<tr>\n<td>{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} => Linha {linha} do arquivo {file} inválida. Valor: {value.ToString("C")}\n</td>\n</tr>";
                            }
                            else if (doc.Length < 14)
                            {
                                FileInvalid.Add(l);
                                MailBody += $"\n<tr>\n<td>{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} => Linha {linha} do arquivo {file} inválida. Valor: '{doc}'\n</td>\n</tr>";
                            }
                            else
                            {
                                FileValid.Add(l);
                            }
                            linha++;
                        }

                        if (FileInvalid.Count > 0)  //só executa alguma ação se encontrar algum DN inválido...
                        {
                            string[] actual = file.Split('\\');
                            string actualName = actual.Last();
                            actual = actualName.Split('.');
                            actualName = $"{actual[0]}.{actual[1]}";

                            //FileInvalidName = $"{actualName}.TXT.ERROR.D{DateTime.Now.ToString("yyyyMMdd")}.T{DateTime.Now.ToString("HHmmss")}";   //nome do arquivo com erros...
                            FileInvalidName = $"{actualName}.TXT.ERROR.{procDateFiltered}.{timeFile}";   //nome do arquivo com erros...

                            //cria as pastas caso não existam...
                            if (!Directory.Exists(FolderToBackup)) { Directory.CreateDirectory(FolderToBackup); }
                            if (!Directory.Exists(FolderNonProcessed)) { Directory.CreateDirectory(FolderNonProcessed); }

                            //Verifica se o arquivo já existe na pasta e caso exista, add 3s no timestamp...
                            if (File.Exists(FolderNonProcessed + FileInvalidName))
                            {
                                int tempTime = int.Parse(timeFile) + 3;
                                FileValidName = $"{actualName}.TXT.ERROR.{procDateFiltered}.{tempTime}";
                            }

                            using (StreamWriter sw = new StreamWriter(FolderNonProcessed + FileInvalidName)) //cria o arquivo somente dos banidos...
                            {
                                foreach (string newLines in FileInvalid)
                                {
                                    sw.WriteLine(newLines);
                                }
                            }
                            Log($"Gerado o arquivo com os DNs inválidos em {FolderNonProcessed}{FileInvalidName}...", false);
                            Console.WriteLine($"Gerado o arquivo com os DNs inválidos em {FolderNonProcessed}{FileInvalidName}...");
                            MailBody += $"\n<tr>\n<td>{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} => Gerado o arquivo com os DNs inválidos em {FolderNonProcessed}{FileInvalidName}...\n</td>\n</tr>";

                            //Thread.Sleep(3000); //intervalo de 3s por segurança...

                            //string BkpOriginalFile = $"{actualName}.TXT.ORIGINAL.D{DateTime.Now.ToString("yyyyMMdd")}.T{DateTime.Now.ToString("HHmmss")}";
                            string BkpOriginalFile = $"{actualName}.TXT.ORIGINAL.{procDateFiltered}.{timeFile}";

                            //Verifica se o arquivo já existe na pasta e caso exista, add 3s no timestamp...
                            if (File.Exists(FolderToBackup + BkpOriginalFile))
                            {
                                int tempTime = int.Parse(timeFile) + 3;
                                BkpOriginalFile = $"{actualName}.TXT.ORIGINAL.{procDateFiltered}.{tempTime}";
                            }

                            File.Move(file, FolderToBackup + BkpOriginalFile);    //renomeia o arquivo original para não ser processado...
                            Log($"Arquivo {actualName}.TXT renomeado e movido para {FolderToBackup}{BkpOriginalFile}...", false);
                            Console.WriteLine($"Arquivo {actualName}.TXT renomeado e movido para {FolderToBackup}{BkpOriginalFile}...");
                            MailBody += $"\n<tr>\n<td>{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} => Arquivo {actualName}.TXT renomeado e movido para {FolderToBackup}{BkpOriginalFile}...\n</td>\n</tr>";

                            //Thread.Sleep(3000); //intervalo de 3s por segurança...

                            //FileWithoutBanName = $"{actualName}.TXT.SB.D{DateTime.Now.ToString("yyyyMMdd")}.T{DateTime.Now.ToString("HHmmss")}";   //nome do arquivo sem ignorados...
                            FileValidName = $"{actualName}.TXT";   //nome do arquivo sem ignorados...

                            while (File.Exists(Folder + FileValidName))
                            {
                                int tempTime = int.Parse(timeFile) + 3;
                                FileValidName = $"{actualName}.TXT.{procDateFiltered}.{tempTime}";
                            }

                            using (StreamWriter sw = new StreamWriter(Folder + FileValidName)) //cria o arquivo somente sem os banidos...
                            {
                                foreach (string newLines in FileValid)
                                {
                                    sw.WriteLine(newLines);
                                }
                            }
                            Log($"Gerado o arquivo válido em {Folder}{FileValidName}...", false);
                            Console.WriteLine($"Gerado o arquivo válido em {Folder}{FileValidName}...");
                            MailBody += $"\n<tr>\n<td>{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} => Gerado o arquivo válido em {Folder}{FileValidName}...\n</td>\n</tr>\n</table>";
                            MailBody += $"<p><b><i>Atenciosamente.</i></b></p>\n<p><b>Monitoring Team</b></p>\n<p>www.br.atos.net</p><img src='cid:assinatura' />";
                            MailBody += $"<p>This email and the documents attached are confidential and intended solely for the addressee; it may also be privileged. If you receive this e-mail in error, please notify the sender immediately and destroy it. As its integrity cannot be secured on the internet, the Atos group liability cannot be triggered for the message content. Although the snder endeavors to maintain a computer virus-free network, the sender does not warrant that this transmission is virus-free and will not be liable for any damages resulting from any virus transmitted.</p>\n</body>\n</html>";
                            EnviaEMail();
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

            void EnviaEMail()
            {
                string dateMail = DateTime.Now.ToString("dd/MM/yyyy");
                string sender = ConfigurationSettings.AppSettings["SenderMail"];
                string server = ConfigurationSettings.AppSettings["ServerMail"];                
                string subject = ConfigurationSettings.AppSettings["SubjectMail"];
                string sign = ConfigurationSettings.AppSettings["SignMail"];
                string assunto = $"{dateMail}{subject}";
                
                try
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(sender);
                    message.Subject = assunto;
                    message.Body = MailBody;
                    message.IsBodyHtml = true;

                    Console.WriteLine("Enviando o E-Mail...");

                    for (int i = 1; i <= 10; i++) 
                    {
                      if (!String.IsNullOrEmpty(ConfigurationSettings.AppSettings[$"RecipientMail{i}"])) 
                        { 
                            message.To.Add(ConfigurationSettings.AppSettings[$"RecipientMail{i}"]); 
                        }                    
                    }

                    SmtpClient smtp = new SmtpClient(server);
                    smtp.Credentials = new NetworkCredential(sender, "");

                    // Adicionar a imagem à mensagem como recurso
                    LinkedResource signRes = new LinkedResource(sign, MediaTypeNames.Image.Jpeg);
                    signRes.ContentId = "assinatura";
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(MailBody, Encoding.UTF8, MediaTypeNames.Text.Html);
                    htmlView.LinkedResources.Add(signRes);
                    message.AlternateViews.Add(htmlView);

                    smtp.Send(message);

                    Console.WriteLine("Relatório enviado com sucesso");
                    Log("Relatório enviado com sucesso", false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Falha ao enviar o relatório... {ex.Message}");
                    Log($"Falha ao enviar o relatório... {ex.Message}", false);
                }
            }

        }
    }

}

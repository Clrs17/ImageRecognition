using ChineseMedicineClassificationModel;
using System.IO;
using System.Collections.Generic;
using System.Linq;

Console.WriteLine("请输入测试集文件夹地址");
string testSetFolderPath = Console.ReadLine();
DirectoryInfo testSetFolder = new DirectoryInfo(testSetFolderPath);

if (!testSetFolder.Exists)
{
    Console.WriteLine("错误：测试集文件夹不存在");
    return;
}

int correctCount = 0;
int totalCount = 0;
List<string> errors = new List<string>();

// 遍历所有分类子文件夹
foreach (var classFolder in testSetFolder.GetDirectories())
{
    string trueLabel = classFolder.Name;
    FileInfo[] imageFiles = classFolder.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                                    .Where(f => f.Extension.ToLower() is ".jpg" or ".jpeg" or ".png" or ".bmp").ToArray();

    Console.WriteLine($"正在处理分类 [{trueLabel}]，共 {imageFiles.Length} 张图片");

    foreach (var imageFile in imageFiles)
    {
        try
        {
            // 读取图片文件
            var imageBytes = File.ReadAllBytes(imageFile.FullName);

            // 创建模型输入
            ChineseMedicineClassification.ModelInput sampleData = new ChineseMedicineClassification.ModelInput()
            {
                ImageSource = imageBytes,
            };

            // 进行预测
            var sortedScoresWithLabel = ChineseMedicineClassification.PredictAllLabels(sampleData);

            // 获取最高分标签
            string predictedLabel = sortedScoresWithLabel
                .FirstOrDefault(score => score.Value == sortedScoresWithLabel.Max(score => score.Value)).Key;

            // 统计结果
            totalCount++;
            if (predictedLabel == trueLabel)
            {
                correctCount++;
            }
            else
            {
                errors.Add($"[错误] 文件: {imageFile.Name} 真实: {trueLabel} 预测: {predictedLabel}");
            }

            // 显示进度（每50个文件显示一次）
            if (totalCount % 50 == 0)
            {
                Console.WriteLine($"共已处理 {totalCount} 张，当前准确率: {correctCount * 100f / totalCount:F2}%");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理文件 {imageFile.Name} 时出错: {ex.Message}");
        }
    }
}

// 输出最终结果
Console.WriteLine("\n" + new string('-', 50));
Console.WriteLine($"总测试图片数: {totalCount}");
Console.WriteLine($"正确预测数: {correctCount}");
Console.WriteLine($"准确率: {correctCount * 100f / totalCount:F2}%");

if (errors.Count > 0)
{
    Console.WriteLine("\n错误案例列表：");
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}

Console.WriteLine("\n按任意键退出...");
Console.ReadKey();
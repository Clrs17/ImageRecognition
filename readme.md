项目概述
-------

本项目是一个基于人工智能技术的中药图片识别系统，利用深度学习模型识别20种常见中药材。解决方案包含三个核心模块，使用.NET 9和WinML：

1. **ChineseMedicineClassificationModel** - 预训练模型模块

2. **ImageClassificationApp** - 跨平台图像分类应用（UWP/WinUI2）

3. **ImageClassificationTest** - 自动化测试程序
   **技术栈**：.NET 9、WinML (ML.NET Vision)、UWP + WinUI2

项目内置的模型系统支持20类中药材的智能识别，采用迁移学习优化的ImageClassificationMulti模型架构，在16C32T 5GHz AMD R9-7940HX CPU上完成训练优化。

解决方案基本结构：

```
ChineseMedicineRecognition/
├── ChineseMedicineClassificationModel/  # 模型模块
|   └── ChineseMedicineClassification.mbconfig # 模型训练数据
│       ├── ChineseMedicineClassification.mlnet  # 预训练模型
│       ├── ChineseMedicineClassification.consumption.cs  # 模型输入/输出类定义
│       └── ChineseMedicineClassification.training.cs  # 模型训练代码
│
├── ImageClassificationApp/  # 应用模块 (UWP)
│   ├── Assets # 应用图标等文件
│   ├── App.xaml  # 应用入口Xaml定义
│   ├── App.xaml.cs  # 应用入口
│   ├── MainPage.xaml  # 用户界面
│   ├── MainPage.xaml.cs  # 业务逻辑
│   └── Package.appxmanifest  # 应用程序包打包信息
│
└── ImageClassificationTest/  # 测试模块 (控制台)
    └── Program.cs  # 模型测试和评估逻辑
```

## 开发环境与依赖

- Windows 10/11  
- [.NET 9 SDK](https://dotnet.microsoft.com/download)  
- Visual Studio 2022（含 UWP/WinUI2 工具集）  
- ML.NET 3 (WinML)  

## 核心模块详解

### 1. 模型模块 - ChineseMedicineClassificationModel

该模块包含预训练好的中药分类模型，使用 ML.NET Vision 的 `ImageClassification` Trainer（Multi-class），训练目标为最大化 **LogLoss-Reduction**，并在 AMD R9-7940HX CPU 上完成训练。

#### 1.1 模型规格

* **数据集**：20分类×75张/类（自动8:2划分）

* **输入格式**：JPEG/PNG/BMP图像字节流

* **输出结果**：分类标签+置信度分数数组

* **训练配置**：本地AMD R9-7940HX CPU训练

#### 1.2 模型性能

* **训练性能**：本地CPU上约耗时420s

* **训练参数**：最终模型的对数损失减少值$\frac{LogLoss(Random Classification)-LogLoss(ModelClassification)}{LogLoss(Random Classification)}=0.8717$

* **分类性能**：本地CPU上分类300张测试集中图片共耗时80s，平均每张图片耗时0.267s，准确率达到89%.

#### 1.3 关键接口

* **训练方式**：
  
  ```csharp
   var pipeline = mlContext.Transforms
        .Conversion.MapValueToKey("Label", "Label")
        .Append(mlContext.MulticlassClassification
            .Trainers.ImageClassification(
                labelColumnName: "Label",
                scoreColumnName: "Score",
                featureColumnName: "ImageSource"))
        .Append(mlContext.Transforms
            .Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));
  
    // 训练模型
    ITransformer model = pipeline.Fit(trainData);
  
    // 保存模型
    mlContext.Model.Save(model, trainData.Schema, "ChineseMedicineClassification.mlnet");
  ```

* **推理示例**：
  
  ```csharp
  
  var input = new ChineseMedicineClassification.ModelInput {
      ImageSource = File.ReadAllBytes("test.jpg")
  };
  var result = ChineseMedicineClassification.Predict(input);
  Console.WriteLine($"Predicted: {result.PredictedLabel}");
  
  ```

### 2. 应用模块 - ImageClassificationApp

#### 2.1 功能特性

1. **多种图片输入方式**
   
   * 文件选择器选取图片
   
   * 拖放图片到识别区域

2. **实时预览**
   
   * 自动显示选中/拖放的图片
   
   * 清空功能快速重置界面

3. **智能分类**
   
   * 自动调用预训练模型进行预测
   
   * 显示TOP1预测结果
   
   * 展示所有类别的置信度分布

#### 2.2 界面实现

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="100"/>
        <RowDefinition/>
    </Grid.RowDefinitions>

    <Button Content="选择图片并分类" Click="PickPicture_Click"/>
    <Button Content="清空图片" Grid.Column="1" Click="ClearPicture_Click"/>

    <Border Grid.Row="1" Grid.ColumnSpan="2">
        <Grid AllowDrop="True" DragOver="ImageDragSystem_DragOver" Drop="ImageDragSystem_Drop">
            <Image x:Name="ImageShow"/>
            <TextBlock Text="将图片拖拽到此处" x:Name="DragAvailableIndicator"/>
            <StackPanel x:Name="RecognizingIndicator" Visibility="Collapsed">
                <ProgressRing IsActive="True"/>
                <TextBlock Text="识别中"/>
            </StackPanel>
        </Grid>
    </Border>
</Grid>
```

#### 2.3 核心识别逻辑

```csharp
private async void ClassifyImage(IRandomAccessStream imageStream)
{
    var imageBytes = new byte[imageStream.Size];
    await imageStream.ReadAsync(imageBytes.AsBuffer(), (uint)imageStream.Size);

    var sampleData = new ChineseMedicineClassification.ModelInput()
    {
        ImageSource = imageBytes
    };

    var sortedScores = ChineseMedicineClassification.PredictAllLabels(sampleData);
    ClassificationResult = "最有可能的种类是：" +
    sortedScoresWithLabel.FirstOrDefault(score => score.Value == sortedScoresWithLabel.Max(score => score.Value)).Key;
    // 获得极大似然估计
}
```

### 3. 测试模块 - ImageClassificationTest

#### 3.1 测试流程

1. 输入测试集路径

2. 遍历子文件夹（对应真实分类，需为文件夹名称）

3. 批量预测并统计，记录错误分类案例并实时展示进度

```csharp
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
```

#### 3.2 输出示例

    正在处理分类 [僵蚕]，共 15 张图片
    2025-05-28 16:35:48.127727: I tensorflow/core/platform/cpu_feature_guard.cc:142] This TensorFlow binary is optimized with oneAPI Deep Neural Network Library (oneDNN)to use the following CPU instructions in performance-critical operations:  AVX2
    To enable them in other operations, rebuild TensorFlow with the appropriate compiler flags.
    2025-05-28 16:35:48.142623: I tensorflow/compiler/xla/service/service.cc:168] XLA service 0x20e007db340 initialized for platform Host (this does not guarantee that XLA will be used). Devices:
    2025-05-28 16:35:48.142687: I tensorflow/compiler/xla/service/service.cc:176]   StreamExecutor device (0): Host, Default Version
    正在处理分类 [党参]，共 15 张图片
    正在处理分类 [天南星]，共 15 张图片
    正在处理分类 [枸杞]，共 15 张图片
    共已处理 50 张，当前准确率: 92.00%
    正在处理分类 [槐花]，共 15 张图片
    正在处理分类 [牛蒡子]，共 15 张图片
    正在处理分类 [牡丹皮]，共 15 张图片
    共已处理 100 张，当前准确率: 94.00%
    正在处理分类 [猪苓]，共 15 张图片
    正在处理分类 [甘草]，共 15 张图片
    正在处理分类 [百合]，共 15 张图片
    共已处理 150 张，当前准确率: 90.00%
    正在处理分类 [百部]，共 15 张图片
    正在处理分类 [竹叶]，共 15 张图片
    正在处理分类 [竹茹]，共 15 张图片
    正在处理分类 [紫草]，共 15 张图片
    共已处理 200 张，当前准确率: 89.00%
    正在处理分类 [红藤]，共 15 张图片
    正在处理分类 [艾叶]，共 15 张图片
    正在处理分类 [荆芥]，共 15 张图片
    共已处理 250 张，当前准确率: 90.00%
    正在处理分类 [金银花]，共 15 张图片
    正在处理分类 [黄柏]，共 15 张图片
    正在处理分类 [黄芪]，共 15 张图片
    共已处理 300 张，当前准确率: 89.00%
    
    --------------------------------------------------
    总测试图片数: 300
    正确预测数: 267
    准确率: 89.00%
    
    错误案例列表：
    [错误] 文件: 10015.jpg 真实: 党参 预测: 黄柏
    [错误] 文件: 10006.jpeg 真实: 天南星 预测: 百部
    [错误] 文件: 10007.jpeg 真实: 天南星 预测: 百部
    [错误] 文件: 10010.jpg 真实: 天南星 预测: 百部
    [错误] 文件: 10012.jpg 真实: 枸杞 预测: 党参
    [错误] 文件: 10010.jpg 真实: 牛蒡子 预测: 艾叶
    [错误] 文件: 10014.jpg 真实: 牡丹皮 预测: 黄芪
    [错误] 文件: 10007.jpeg 真实: 猪苓 预测: 天南星
    [错误] 文件: 10008.jpeg 真实: 猪苓 预测: 天南星
    [错误] 文件: 10012.jpg 真实: 猪苓 预测: 艾叶
    [错误] 文件: 10001.jpeg 真实: 甘草 预测: 槐花
    [错误] 文件: 10007.jpeg 真实: 甘草 预测: 百部
    [错误] 文件: 10009.jpg 真实: 甘草 预测: 党参
    [错误] 文件: 10001.jpeg 真实: 百合 预测: 猪苓
    [错误] 文件: 10015.jpg 真实: 百合 预测: 黄芪
    [错误] 文件: 10001.jpeg 真实: 百部 预测: 党参
    [错误] 文件: 10001.jpeg 真实: 竹叶 预测: 艾叶
    [错误] 文件: 10015.jpeg 真实: 竹叶 预测: 艾叶
    [错误] 文件: 10006.jpeg 真实: 竹茹 预测: 金银花
    [错误] 文件: 10007.jpeg 真实: 竹茹 预测: 金银花
    [错误] 文件: 10011.jpeg 真实: 竹茹 预测: 金银花
    [错误] 文件: 10004.jpeg 真实: 紫草 预测: 艾叶
    [错误] 文件: 10014.jpeg 真实: 紫草 预测: 艾叶
    [错误] 文件: 10015.jpeg 真实: 紫草 预测: 黄柏
    [错误] 文件: 10012.jpeg 真实: 红藤 预测: 艾叶
    [错误] 文件: 10014.jpeg 真实: 荆芥 预测: 天南星
    [错误] 文件: 10004.jpeg 真实: 金银花 预测: 僵蚕
    [错误] 文件: 10013.jpg 真实: 金银花 预测: 艾叶
    [错误] 文件: 10005.jpeg 真实: 黄柏 预测: 党参
    [错误] 文件: 10006.jpeg 真实: 黄柏 预测: 百部
    [错误] 文件: 10014.jpeg 真实: 黄柏 预测: 黄芪
    [错误] 文件: 10009.jpg 真实: 黄芪 预测: 黄柏
    [错误] 文件: 10011.jpeg 真实: 黄芪 预测: 僵蚕

4.使用指南
----

### 运行要求

* Windows 10 版本 1709 或更高

* .NET 9 SDK

* Visual Studio 2022

### 快速开始

1. 克隆仓库到本地

2. 使用Visual Studio打开解决方案文件

3. 设置`ImageClassificationApp`为启动项目

4. 按F5运行应用程序

### 测试模型性能

1. 准备测试集（按类别分组的图片文件夹）

2. 运行`ImageClassificationTest`项目

3. 输入测试集文件夹路径

4. 查看终端输出的准确率和错误案例

5.扩展与定制
-----

### 模型重新训练

```csharp
public static ITransformer RetrainModel(MLContext mlContext, IDataView trainData)
{
    var pipeline = BuildPipeline(mlContext); 
    return pipeline.Fit(trainData);
}
```

### 模型替换指南

1. 在`ChineseMedicineClassificationModel`项目中替换`.mlnet`模型文件（可使用现成的模型或自行训练）

2. 更新`ModelInput`和`ModelOutput`类以匹配新模型结构

3. 重新编译解决方案

6.常见问题解决
------

1. **图片加载失败**：确保图片格式为JPG/PNG/BMP

2. **模型加载错误**：检查`ChineseMedicineClassification.mlnet`是否在输出目录

3. **分类结果不准确**：尝试重新训练模型或增加训练数据量

6.参考链接
-------

* [ML.NET 图像分类文档](https://docs.microsoft.com/dotnet/machine-learning/tutorials/image-classification)
* [WinML 技术](https://learn.microsoft.com/windows/ai/)
* [.NET 9 UWP应用开发](https://devblogs.microsoft.com/ifdef-windows/preview-uwp-support-for-dotnet-9-native-aot/)

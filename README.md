# TypoChecker

基于AI的拼写检查工具（错别字检查工具），支持连接本地Ollama API或在线OpenAI API。

## 配置选项

### 服务选择

可以选择使用以下两种 AI 服务之一：

- OpenAI：使用与OpenAI API兼容的在线服务（如GPT系列、DeepSeek系列等）
- Ollama：使用本地或自托管的 Ollama 服务

### OpenAI 配置

当选择 OpenAI 时，需要配置以下参数：

- 地址：API的地址。例如：`https://api.openai.com/v1` (OpenAI)、`https://api.deepseek.com/v1` (DeepSeek)
- Key：服务密钥，由服务提供商提供
- 模型：要使用的模型名称。例如：`gpt-4-turbo` (GPT4)、`deepseek-chat` (DeepSeek V3)

### Ollama 配置

当选择 Ollama 时，需要配置以下参数：

- 地址：Ollama服务的地址。本地默认值：`http://localhost:11434/api/generate`
- 模型：要使用的 Ollama 模型名称

### 文本处理参数

- 分段字数阈值：控制文本分段检查的大小。较大的值会提高检查速度但可能降低精度，较小的值会提高精度但降低速度。建议范围为 `300~2000`
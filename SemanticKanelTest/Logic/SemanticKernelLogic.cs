using Markdig;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace SemanticKanelTest.Logic
{
    public class SemanticKernelLogic: ISemanticKernelLogic
    {
        private readonly ILogger<SemanticKernelLogic> _logger;
        private readonly IConfiguration _configuration;
        private ChatHistory chatHistory;

        public IChatCompletion ChatCompletion { get; set; }
        public string GeneratedHtml { get; set; } = string.Empty;

        public SemanticKernelLogic(ILogger<SemanticKernelLogic> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            string deploymentName = _configuration.GetValue<string>("DeploymentName") ?? string.Empty;
            string baseUrl = _configuration.GetValue<string>("BaseUrl") ?? string.Empty;
            string key = _configuration.GetValue<string>("Key") ?? string.Empty;

            IKernel kernel = new KernelBuilder()
                .WithLogger(_logger)
                .WithAzureChatCompletionService(deploymentName, baseUrl, key)
                .Build();

            ChatCompletion = kernel.GetService<IChatCompletion>();
            var prompt = "あなたはうそまるというキャラクターです。ユーザーの質問に対して語尾には必ず「ぴょん」をつけて回答してください。";
            chatHistory = ChatCompletion.CreateNewChat(prompt);
        }

        public void ClearChatHistory()
        {
            chatHistory = ChatCompletion.CreateNewChat("あなたはうそまるというキャラクターです。ユーザーの質問に対して語尾には必ず「ぴょん」をつけて回答してください。");
        }

        public async Task StreamRun(string input)
        {
            chatHistory.AddUserMessage(input);
            ChatRequestSettings settings = new ChatRequestSettings();
            settings.MaxTokens = 2000;

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseAutoLinks().UseBootstrap().UseDiagrams().UseGridTables().Build();

            string message = await ChatCompletion.GenerateMessageAsync(chatHistory, settings);
            GeneratedHtml = Markdown.ToHtml(message, pipeline);
            NotifyStateChanged();
            chatHistory.AddAssistantMessage(message);
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
        public event Action? OnChange;
    }
}

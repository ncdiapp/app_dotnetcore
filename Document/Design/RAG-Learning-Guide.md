# RAG 学习指南（Retrieval-Augmented Generation）

> 目标：用最短路径弄清 RAG 是什么、怎么跑、以及两个可落地的例子  
> （AI 知识库问答 / 体育比赛视频分析）

---

## 1. 一句话定义

**RAG = 先检索相关资料，再让大模型基于这些资料生成回答。**

全称：**Retrieval-Augmented Generation**（检索增强生成）。

| 只靠模型记忆 | 加上 RAG |
|---|---|
| 容易“编造”公司内部规定、最新比分 | 答案锚定在你提供的文档/片段上 |
| 知识截止于训练数据 | 可随时更新知识库，不必重训模型 |
| 说不清出处 | 可返回引用片段（页码、文件名、时间戳） |

和你直觉里的「切片 → 准备 → 再分析」一致：  
**切块（Chunk）→ 向量化（Embed）→ 检索（Retrieve）→ 生成（Generate）**。

---

## 2. 标准流水线（必记四步）

```text
┌──────────────┐   ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│ 1. Ingest    │ → │ 2. Index     │ → │ 3. Retrieve  │ → │ 4. Generate  │
│ 清洗/切片    │   │ Embedding    │   │ Top-K 相似段 │   │ LLM + 上下文 │
└──────────────┘   └──────────────┘   └──────────────┘   └──────────────┘
      离线/准实时                          ↑ 在线查询时触发
```

### 2.1 Ingest（入库准备）

- 文档：PDF、Markdown、Wiki、工单、FAQ  
- 清洗：去页眉页脚、表格转文本、OCR  
- **Chunking（切片）**：按段落 / 固定 token / 标题层级切开  
- 常见技巧：
  - **Overlap（重叠）**：相邻块重叠 10%～20%，避免句子被切断丢语义  
  - **元数据**：`source`, `page`, `section`, `updated_at` 一并保存，方便引用与过滤

### 2.2 Index（建索引）

- 用 **Embedding 模型**把每块文本变成向量（一串浮点数）  
- 向量库（或内存余弦相似度）按相似度检索  
- 可选：**关键词索引（BM25）+ 向量检索 = Hybrid Search（混合检索）**

### 2.3 Retrieve（检索）

1. 用户问题 → 同样做 Embedding  
2. 在库中找最相似的 Top-K 块（如 K=5）  
3. 可选：**Rerank（重排）**，用更强模型精排，提高精度  

### 2.4 Generate（生成）

把检索到的片段拼进 Prompt，例如：

```text
你只能根据【资料】回答。资料不足时说“资料中未找到”。
并列出引用（文件名 + 段落）。

【资料】
[1] ...chunk A...
[2] ...chunk B...

【问题】
员工出差住宿标准是多少？
```

模型输出答案 + 引用，而不是凭空猜。

---

## 3. 核心名词速查

| 术语 | 含义 | 类比 |
|---|---|---|
| Chunk | 切好的文本块 | 书页上的一段 |
| Embedding | 文本→向量 | 给每段打“语义指纹” |
| Vector Store | 存向量并做近邻搜索 | 指纹图书馆 |
| Top-K | 取最相关的 K 条 | 只翻最相关的 5 页 |
| Hybrid Search | 语义 + 关键词一起搜 | 既懂意思又抓专有名词 |
| Rerank | 二次精排 | 粗筛后再人工挑最好的 |
| Grounding | 答案有据可查 | “说话要有出处” |
| Hallucination | 模型编造 | 没资料却写得很肯定 |

---

## 4. 例子 A：AI 知识库查询（企业 FAQ / 制度问答）

### 4.1 场景

员工问：「出差到上海，酒店上限多少？需要谁审批？」

知识库里有：`差旅政策.pdf`、`审批流程.md`、`FAQ.xlsx`。

### 4.2 离线准备（一次，或文档更新时重做）

```text
差旅政策.pdf
  → 按「章节标题 + 500 tokens」切片，overlap=80
  → 每块附带 metadata: {file, page, section}
  → Embedding 写入向量库 collection=hr_policy
```

伪代码：

```python
# 概念示例（非生产代码）
chunks = []
for doc in load_docs("knowledge/hr"):
    for i, text in enumerate(split(doc.text, size=500, overlap=80)):
        chunks.append({
            "id": f"{doc.id}_{i}",
            "text": text,
            "meta": {"source": doc.name, "page": doc.page_of(i)}
        })

vectors = embed([c["text"] for c in chunks])   # 调用 Embedding API
index.upsert(chunks, vectors)                  # 写入向量库
```

### 4.3 在线查询

```text
Q: 上海出差酒店上限？谁审批？
  → embed(Q)
  → vector_search(Top-K=5) + keyword_search("上海" OR "住宿")
  → merge / rerank
  → prompt = 系统指令 + 5 个 chunk + Q
  → LLM 回答：
      「一线城市住宿上限 ¥650/晚（资料[1]）。
       超过标准需直属经理 + 财务审批（资料[2]）。」
```

### 4.4 好答案长什么样

- **可引用**：`差旅政策.pdf p.12`  
- **可拒答**：资料没有时明确说“未找到”，而不是编造  
- **可过滤**：按 `department=HR`、`effective_date>=2025` 缩小范围  

### 4.5 常见坑

| 问题 | 现象 | 处理思路 |
|---|---|---|
| 切太碎 | 上下文残缺 | 增大 chunk 或按标题切 |
| 切太大 | 检索噪音多 | 减小 chunk + rerank |
| 只向量检索 | “PO号”“SKU”这类专名搜不准 | Hybrid（向量 + 关键词） |
| 资料过期 | 答旧政策 | 元数据里加版本/生效日，检索时过滤 |
| 上下文塞太满 | 超 token / 模型分心 | 控制 Top-K，或先摘要再生成 |

---

## 5. 例子 B：体育比赛视频分析（多模态 RAG）

视频不能直接整段塞进 LLM。做法是把视频变成**可检索的“事件片段 + 文本描述”**，再 RAG。

### 5.1 场景

教练问：「下半场第 67 分钟左右，主队右路传中后的射门过程是怎样的？谁漏人了？」

### 5.2 把视频变成知识库（关键）

```text
比赛录像 90 分钟
  │
  ├─ ① 时间切片：每 2～5 秒一帧 / 按镜头切段
  ├─ ② 语音转写：解说/麦克风 → 字幕时间轴（ASR）
  ├─ ③ 视觉理解：帧/片段 → 事件标签
  │      例：shot, pass, foul, corner, offside, player#7
  ├─ ④ 结构化事件：
  │      {t_start, t_end, event, players, zone, description}
  └─ ⑤ Embedding：对 description（+可选图像向量）建索引
```

一条入库记录示例：

```json
{
  "id": "match20260320_e1842",
  "t_start": "00:66:48",
  "t_end": "00:67:12",
  "event": "shot",
  "team": "home",
  "players": ["#9 striker", "#11 RW", "#4 CB away"],
  "zone": "right_wing → box",
  "description": "主队右路传中，禁区前沿抢点射门偏出；客队中卫失位未贴紧。",
  "clip_url": "clips/1842.mp4",
  "keyframe": "frames/1842.jpg"
}
```

### 5.3 查询时怎么走

```text
用户问题
  → 文本 Embedding（也可加时间过滤：下半场 / 60–75 分钟）
  → 检索 Top-K 事件段 + 对应字幕片段
  → （可选）把关键帧/短视频再喂给多模态模型做细读
  → 生成战术文字分析 + 时间戳链接
```

回答示例：

> 约 **66:52–67:08**：主队 11 号右路传中，9 号禁区前沿抢点偏出。  
> 客队 4 号在传中瞬间未跟上 9 号（见 clip `e1842`）。  
> 相关解说字幕：「机会！球偏出！」

### 5.4 和文本知识库的差异

| 维度 | 文档 RAG | 视频比赛 RAG |
|---|---|---|
| 切片单位 | 段落 / token | 时间段 / 事件 / 镜头 |
| 主键 | 文件+页码 | 时间戳 + 事件 ID |
| Embedding | 文本向量为主 | 文本 + 可选图像/视频向量 |
| 生成输入 | 文本 chunk | 事件描述 + 字幕 +（可选）关键帧 |
| 用户体验 | 引用页码 | 跳到录像时间点 |

### 5.5 最小可行版本（MVP）

不必一上来就上复杂视觉模型：

1. 解说音轨 → ASR 字幕（带时间）  
2. 字幕按 30～60 秒切块 + Embedding  
3. 问「谁进球」时先检索字幕，再人工/模型对齐进球时间  
4. 进阶：加事件检测模型（射门、角球）做结构化过滤  

---

## 6. 端到端对照（两个例子同一套路）

```text
                知识库 FAQ                    比赛视频分析
Ingest     PDF/MD 切段                   视频→事件/字幕切段
Index      文本 Embedding                文本(+图像) Embedding
Retrieve   “上海住宿标准” → Top-K 政策段   “67分钟右路传中” → Top-K 事件
Generate   制度答案 + 页码引用            战术解说 + 时间戳/片段链接
```

**记住：RAG 的灵魂是“检索到的上下文”，不是换一个更贵的模型。**

---

## 7. 最小 Prompt 模板（可直接改着用）

```text
角色：你是严谨的助手，只能使用【检索结果】中的信息。
规则：
1. 每条关键结论后标注引用编号，如 [1]。
2. 检索结果不足以回答时，回复「资料不足」，不要猜测。
3. 不要编造检索结果里没有的数字、人名、时间。

【检索结果】
[1] source=... | ...内容...
[2] source=... | ...内容...

【用户问题】
{question}

【输出格式】
- 直接答案
- 依据（引用编号）
- 不确定点（如有）
```

---

## 8. 评估：怎样算“学懂了 / 系统可用”

用 20～50 条真实问题做小测评：

| 指标 | 含义 |
|---|---|
| Hit@K | Top-K 里是否包含正确答案所在片段 |
| Faithfulness | 答案是否忠于检索内容（不胡编） |
| Answer Relevance | 是否真正回答了问题 |
| Citation Accuracy | 引用是否指对了段落/时间戳 |

自测题（口头答即可）：

1. 为什么要 Chunk + Overlap？  
2. 为什么专有名词场景常用 Hybrid Search？  
3. 视频 RAG 的“chunk”通常是什么？  
4. 检索很好但答案仍错，问题更可能在哪一步？

参考答案要点：

1. 控制长度、保留边界语义。  
2. 向量偏语义，关键词抓精确字符串。  
3. 时间段 / 事件描述 / 字幕窗。  
4. Prompt、上下文过长、模型忽略指令，或检索到的是“相关但非正解”的段。

---

## 9. 和本仓库的关系（对照学习）

本仓库 **MCP Gateway** 里有一条偏工程的 RAG 路径：对 **Swagger/OpenAPI 接口目录**做语义检索，再配合关键词，形成 Hybrid Search。

相关代码（便于对照概念，不是完整通用知识库）：

| 概念 | 仓库中的近似实现 |
|---|---|
| Embedding 提供者 | `APP.McpGateway/Services/IEmbeddingProvider.cs` |
| 向量索引 / 余弦相似度 | `APP.McpGateway/Services/SemanticSearchService.cs` |
| Hybrid（语义 + 关键词） | `ISwaggerService.HybridSearchAsync` |
| 工具侧入口 | MCP tool：`semantic_search_endpoints`（见 `Document/mcp/mcp-gateway-architecture.md`） |

这里的“文档”是 **API endpoint 描述**，不是 PDF；但流水线仍然是：

**文本 → Embedding → 相似度检索 →（再交给 Agent/LLM 使用）**。

Agent 侧也有 backlog：工具目录可用 Embedding 做 Tool RAG（见 `Document/AgentDesign/ThinkMode-ToolsPrefer-Plan.md`）。

---

## 10. 延伸：RAG 的亲戚（别混淆）

| 名称 | 和 RAG 的关系 |
|---|---|
| Fine-tuning（微调） | 改模型权重；RAG 是改“输入上下文” |
| Long Context | 把整本书塞进窗口；贵且易稀释，常与 RAG 互补 |
| Agent + Tools | Agent 可先调用搜索/数据库工具，本质也是“先取证再生成” |
| GraphRAG | 用知识图谱增强实体关系检索 |
| Multimodal RAG | 图文音视频都能检索（比赛分析属于这类） |

---

## 11. 建议学习路径（1～2 天）

1. **半天概念**：把本文 §1–§3 讲给别人听（不看稿）。  
2. **半天动手（知识库）**：拿 3～5 篇 Markdown，本地切块 + 任意向量库 + 一个问答 Prompt。  
3. **半天动手（视频 MVP）**：一场比赛解说音 → ASR → 按时间切块检索。  
4. **对照本仓库**：读 `SemanticSearchService` 的 cache / cosine 流程，对照 §2。  

---

## 12. 速记卡片

```text
RAG = Retrieve 相关块 + Augment 进 Prompt + Generate 答案

知识库：文档切片 → 向量 → 问答引用页码
比赛视频：事件/字幕切片 → 向量 → 分析引用时间戳

质量三件套：切得好、检索准、Prompt 约束严（不足则拒答）
```

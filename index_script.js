const fs = require('fs/promises');
const path = require('path');
const { GoogleGenAI } = require(path.join(require('os').homedir(), ".pi/agent/extensions/smart-rag/node_modules/@google/genai"));

const apiKey = "AIzaSyCVBcu9kIiNWICC1NJvrCceobJSTJ4dWF8";
const DB_PATH = path.join(require('os').homedir(), ".pi/agent/extensions/smart-rag/vector_db.json");
const ai = new GoogleGenAI({ apiKey });

async function findFiles(dir, fileList = []) {
  const files = await fs.readdir(dir, { withFileTypes: true });
  for (const file of files) {
    const fullPath = path.join(dir, file.name);
    if (file.isDirectory() && !fullPath.includes("node_modules") && !fullPath.includes(".git") && !fullPath.includes(".pi") && !fullPath.includes("bin") && !fullPath.includes("obj")) {
      await findFiles(fullPath, fileList);
    } else if (file.isFile() && (file.name.endsWith(".cs") || file.name.endsWith(".razor") || file.name.endsWith(".md"))) {
      fileList.push(fullPath);
    }
  }
  return fileList;
}

async function run() {
  console.log("Finding files...");
  const files = await findFiles("C:/Dev/6IA-IT-Portal");
  console.log(`Found ${files.length} files.`);
  
  let chunks = [];
  for (const file of files) {
    const content = await fs.readFile(file, "utf-8");
    if (content.trim() === "") continue;
    
    // Chunk by file for simplicity, or rough splitting
    const maxLen = 4000;
    if (content.length <= maxLen) {
      chunks.push({ id: file + "-0", filePath: file, content, type: "file" });
    } else {
      let part = 0;
      for (let i = 0; i < content.length; i += maxLen) {
        chunks.push({ id: file + "-" + part++, filePath: file, content: content.slice(i, i + maxLen), type: "file" });
      }
    }
  }
  
  console.log(`Generated ${chunks.length} chunks. Fetching embeddings...`);
  
  let batchSize = 5;
  for (let i = 0; i < chunks.length; i += batchSize) {
    const batch = chunks.slice(i, i + batchSize);
    try {
      const promises = batch.map(c => ai.models.embedContent({ model: "gemini-embedding-2", contents: c.content }));
      const responses = await Promise.all(promises);
      batch.forEach((c, idx) => {
        if (responses[idx] && responses[idx].embeddings && responses[idx].embeddings.length > 0) {
          c.embedding = responses[idx].embeddings[0].values;
        }
      });
      console.log(`Embedded ${i + batch.length} / ${chunks.length}`);
    } catch (err) {
      console.error(`Error on batch ${i}:`, err.message);
    }
  }
  
  // Also load existing chunks to preserve them
  let existing = [];
  try {
    existing = JSON.parse(await fs.readFile(DB_PATH, "utf-8"));
  } catch (e) {}
  
  const allChunks = existing.concat(chunks.filter(c => c.embedding));
  await fs.writeFile(DB_PATH, JSON.stringify(allChunks, null, 2));
  console.log("Saved vector db.");
}

run().catch(console.error);
import { dotnet } from './_framework/dotnet.js'

const dropZone = document.getElementById('drop-zone');
const folderInput = document.getElementById('folder-input');
const startBtn = document.getElementById('start-btn');
const out = document.getElementById('out');
const setupContainer = document.getElementById('setup-container');
const canvas = document.getElementById('canvas');
const progressBar = document.getElementById('progress-bar');
const progressContainer = document.getElementById('progress-container');
const percentageText = document.getElementById('percentage');

let moduleAPI = null;

// The rest of Emscripten/WASM logic will be injected when we implement IDBFS sync
try {
    const { setModuleImports, getConfig, Module } = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();
    
    moduleAPI = Module;

    out.innerHTML = "WASM Engine Booted. Connecting to Cloud Assets...";
    progressContainer.style.display = 'block';

    // Start automated asset download loop
    const response = await fetch('assets/catalog.json');
    const catalog = await response.json();
    
    let chunks = [];
    let numChunks = 14; 
    
    for(let i = 0; i < numChunks; i++) {
        const percent = Math.round((i/numChunks) * 100);
        out.innerHTML = `Downloading cloud data... Chunk ${i+1}/${numChunks}`;
        progressBar.style.width = percent + "%";
        percentageText.innerText = percent + "%";

        let res = await fetch(`assets/chunk_${i}.bin`);
        chunks[i] = await res.arrayBuffer();
    }
    
    out.innerHTML = "Reconstructing File System...";
    if (!Module.FS.analyzePath('/Content').exists) Module.FS.mkdir('/Content');
    
    let entries = Object.entries(catalog);
    let totalFiles = entries.length;
    let processedFiles = 0;
    
    for (const [path, info] of entries) {
        const parts = path.split('/');
        let currentDir = '/Content';
        for(let j = 0; j < parts.length - 1; j++) {
            currentDir += '/' + parts[j];
            if (!Module.FS.analyzePath(currentDir).exists) Module.FS.mkdir(currentDir);
        }
        
        let bytesLeft = info.length;
        let pChunk = info.chunk_start;
        let pOffset = info.offset_start;
        
        let fileData = new Uint8Array(bytesLeft);
        let dstOffset = 0;
        
        while(bytesLeft > 0) {
            let chunkData = chunks[pChunk];
            let bytesToCopy = Math.min(bytesLeft, chunkData.byteLength - pOffset);
            let view = new Uint8Array(chunkData, pOffset, bytesToCopy);
            fileData.set(view, dstOffset);
            
            dstOffset += bytesToCopy;
            bytesLeft -= bytesToCopy;
            pChunk++;
            pOffset = 0;
        }
        
        Module.FS.writeFile('/Content/' + path, fileData);
        processedFiles++;
        
        if (processedFiles % 100 === 0) {
            const percent = Math.round((processedFiles/totalFiles)*100);
            out.innerHTML = `Finalizing game world... (${processedFiles}/${totalFiles} files)`;
            progressBar.style.width = percent + "%";
            percentageText.innerText = percent + "%";
        }
    }
    
    out.innerHTML = "Launch Sequence Ready! Booting Game...";
    progressBar.style.width = "100%";
    percentageText.innerText = "100%";
    
    // Auto-boot sequence
    setTimeout(async () => {
        setupContainer.style.opacity = '0';
        setTimeout(() => { setupContainer.style.display = 'none'; }, 800);
        
        canvas.style.display = 'block';
        await dotnet.run();
    }, 500);

    // Mount IDBFS for save files
    if (!Module.FS.analyzePath('/home/web_user').exists) Module.FS.mkdir('/home/web_user');
    Module.FS.mount(Module.IDBFS, {}, '/home/web_user');
    Module.FS.syncfs(true, (err) => {
        if(err) console.error("IDBFS init error: ", err);
        else console.log("IDBFS initialized!");
    });
    
    window.SyncSaves = () => {
        if(Module.FS) {
            Module.FS.syncfs(false, (err) => {
                if(err) console.error("Failed to sync save: ", err);
                else console.log("Save synced to IndexedDB!");
            });
        }
    };

} catch (err) {
    out.innerHTML = "Error loading WASM: " + err;
    console.error(err);
}
import { dotnet } from './_framework/dotnet.js'

const out = document.getElementById('out');
const setupContainer = document.getElementById('setup-container');
const canvas = document.getElementById('canvas');
const progressBar = document.getElementById('progress-bar');
const progressContainer = document.getElementById('progress-container');
const percentageText = document.getElementById('percentage');

let moduleAPI = null;

function showError(message) {
    out.innerHTML = `<span style="color:#f44336;">&#9888; ${message}</span>`;
    console.error('[StardewWasm]', message);
}

async function fetchWithRetry(url, retries = 3, delay = 1000) {
    for (let attempt = 1; attempt <= retries; attempt++) {
        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP ${response.status} ${response.statusText}`);
            }
            return response;
        } catch (err) {
            if (attempt === retries) {
                throw new Error(`Failed to fetch "${url}" after ${retries} attempts: ${err.message}`);
            }
            console.warn(`[StardewWasm] Attempt ${attempt} failed for "${url}": ${err.message}. Retrying in ${delay}ms...`);
            await new Promise(resolve => setTimeout(resolve, delay));
        }
    }
}

try {
    const { setModuleImports, getConfig, Module } = await dotnet
        .withDiagnosticTracing(true)
        .withApplicationArgumentsFromQuery()
        .create();

    if (!Module || !Module.FS) {
        throw new Error('WASM Module or Module.FS is not available after initialization.');
    }

    moduleAPI = Module;

    out.innerHTML = "WASM Engine Booted. Connecting to Cloud Assets...";
    progressContainer.style.display = 'block';

    // Fetch and validate catalog
    let catalog;
    try {
        const response = await fetchWithRetry('assets/catalog.json');
        catalog = await response.json();
    } catch (err) {
        showError(`Failed to load asset catalog: ${err.message}`);
        throw err;
    }

    if (!catalog || typeof catalog !== 'object') {
        const err = new Error('Asset catalog is invalid or empty.');
        showError(err.message);
        throw err;
    }

    // Determine chunk count from catalog metadata or fall back to the expected default
    const DEFAULT_CHUNK_COUNT = 14;
    const numChunks = (typeof catalog._chunks === 'number') ? catalog._chunks : DEFAULT_CHUNK_COUNT;

    let chunks = [];

    for (let i = 0; i < numChunks; i++) {
        const percent = Math.round((i / numChunks) * 100);
        out.innerHTML = `Downloading cloud data... Chunk ${i + 1}/${numChunks}`;
        progressBar.style.width = percent + "%";
        percentageText.innerText = percent + "%";

        try {
            const res = await fetchWithRetry(`assets/chunk_${i}.bin`);
            chunks[i] = await res.arrayBuffer();
        } catch (err) {
            showError(`Failed to download chunk ${i}: ${err.message}`);
            throw err;
        }
    }

    out.innerHTML = "Reconstructing File System...";

    try {
        if (!moduleAPI.FS.analyzePath('/Content').exists) moduleAPI.FS.mkdir('/Content');
    } catch (err) {
        showError(`Failed to initialize /Content directory: ${err.message}`);
        throw err;
    }

    let entries = Object.entries(catalog).filter(([key]) => !key.startsWith('_'));
    let totalFiles = entries.length;
    let processedFiles = 0;

    for (const [path, info] of entries) {
        try {
            const parts = path.split('/');
            let currentDir = '/Content';
            for (let j = 0; j < parts.length - 1; j++) {
                currentDir += '/' + parts[j];
                if (!moduleAPI.FS.analyzePath(currentDir).exists) moduleAPI.FS.mkdir(currentDir);
            }

            let bytesLeft = info.length;
            let pChunk = info.chunk_start;
            let pOffset = info.offset_start;

            let fileData = new Uint8Array(bytesLeft);
            let dstOffset = 0;

            while (bytesLeft > 0) {
                if (!chunks[pChunk]) {
                    throw new Error(`Chunk ${pChunk} required for file "${path}" is missing.`);
                }
                let chunkData = chunks[pChunk];
                let bytesToCopy = Math.min(bytesLeft, chunkData.byteLength - pOffset);
                let view = new Uint8Array(chunkData, pOffset, bytesToCopy);
                fileData.set(view, dstOffset);

                dstOffset += bytesToCopy;
                bytesLeft -= bytesToCopy;
                pChunk++;
                pOffset = 0;
            }

            moduleAPI.FS.writeFile('/Content/' + path, fileData);
        } catch (err) {
            showError(`Failed to write file "${path}": ${err.message}`);
            throw err;
        }

        processedFiles++;

        if (processedFiles % 100 === 0) {
            const percent = Math.round((processedFiles / totalFiles) * 100);
            out.innerHTML = `Finalizing game world... (${processedFiles}/${totalFiles} files)`;
            progressBar.style.width = percent + "%";
            percentageText.innerText = percent + "%";
        }
    }

    out.innerHTML = "Launch Sequence Ready! Booting Game...";
    progressBar.style.width = "100%";
    percentageText.innerText = "100%";

    // Mount IDBFS for save files before booting and wait for initial sync
    try {
        if (!moduleAPI.FS.analyzePath('/home/web_user').exists) moduleAPI.FS.mkdir('/home/web_user');
        moduleAPI.FS.mount(moduleAPI.IDBFS, {}, '/home/web_user');
        await new Promise((resolve, reject) => {
            moduleAPI.FS.syncfs(true, (err) => {
                if (err) {
                    console.error('[StardewWasm] IDBFS init error:', err);
                    reject(err);
                } else {
                    console.log('[StardewWasm] IDBFS initialized!');
                    resolve();
                }
            });
        });
    } catch (err) {
        console.warn('[StardewWasm] IDBFS setup failed (save files may not persist):', err.message);
    }

    window.SyncSaves = () => {
        if (moduleAPI && moduleAPI.FS) {
            moduleAPI.FS.syncfs(false, (err) => {
                if (err) console.error('[StardewWasm] Failed to sync save:', err);
                else console.log('[StardewWasm] Save synced to IndexedDB!');
            });
        } else {
            console.warn('[StardewWasm] Cannot sync saves: Module.FS is not available.');
        }
    };

    // Auto-boot sequence
    setTimeout(async () => {
        setupContainer.style.opacity = '0';
        setTimeout(() => { setupContainer.style.display = 'none'; }, 800);

        canvas.style.display = 'block';
        await dotnet.run();
    }, 500);

} catch (err) {
    if (!out.innerHTML.includes('&#9888;')) {
        showError('Error loading WASM: ' + err.message);
    }
    console.error('[StardewWasm] Fatal error:', err);
}
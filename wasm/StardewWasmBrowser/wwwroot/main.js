import { dotnet } from './_framework/dotnet.js'

const dropZone = document.getElementById('drop-zone');
const folderInput = document.getElementById('folder-input');
const startBtn = document.getElementById('start-btn');
const out = document.getElementById('out');
const setupContainer = document.getElementById('setup-container');
const canvas = document.getElementById('canvas');

let moduleAPI = null;

// The rest of Emscripten/WASM logic will be injected when we implement IDBFS sync
try {
    const { setModuleImports, getConfig, Module } = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();
    
    moduleAPI = Module;

    out.innerHTML = "WASM Loaded. Ready for assets.";

    dropZone.addEventListener('dragover', (e) => { e.preventDefault(); dropZone.classList.add('dragover'); });
    dropZone.addEventListener('dragleave', () => dropZone.classList.remove('dragover'));
    
    dropZone.addEventListener('drop', async (e) => {
        e.preventDefault();
        dropZone.classList.remove('dragover');
        out.innerHTML = "Processing files... Please wait.";
        
        try {
            if (!Module.FS.analyzePath('/Content').exists) Module.FS.mkdir('/Content');
            
            let totalFiles = 0; let processedFiles = 0;
            
            function checkDone() {
                if (processedFiles === totalFiles) {
                    out.innerHTML = `Loaded ${totalFiles} assets! Ready to play.`;
                    startBtn.disabled = false;
                    startBtn.innerText = "Start Game";
                }
            }
            
            async function processEntry(entry, currentPath) {
                if (entry.isFile) {
                    totalFiles++;
                    entry.file(async (file) => {
                        const arr = new Uint8Array(await file.arrayBuffer());
                        const dest = currentPath ? `/Content/${currentPath}/${file.name}` : `/Content/${file.name}`;
                        Module.FS.writeFile(dest, arr);
                        processedFiles++;
                        if (processedFiles % 100 === 0) out.innerHTML = `Loaded ${processedFiles} files...`;
                        checkDone();
                    });
                } else if (entry.isDirectory) {
                    const newPath = currentPath ? `${currentPath}/${entry.name}` : entry.name;
                    if (!Module.FS.analyzePath(`/Content/${newPath}`).exists) Module.FS.mkdir(`/Content/${newPath}`);
                    const dirReader = entry.createReader();
                    
                    // readEntries doesn't return all files at once if there are >100, requires looping
                    const readAll = () => {
                        dirReader.readEntries((entries) => {
                            if (entries.length > 0) {
                                entries.forEach(e => processEntry(e, newPath));
                                readAll();
                            } else {
                                checkDone(); // in case folder was empty
                            }
                        });
                    };
                    readAll();
                }
            }
            
            const items = e.dataTransfer.items;
            for (let i = 0; i < items.length; i++) {
                const item = items[i];
                if (item.kind === 'file') {
                    const entry = item.webkitGetAsEntry();
                    if (entry) processEntry(entry, "");
                }
            }
        } catch (err) {
            out.innerHTML = "Error processing files: " + err;
        }
    });

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

    startBtn.onclick = async () => {
        setupContainer.style.display = 'none';
        canvas.style.display = 'block';
        await dotnet.run();
    };

} catch (err) {
    out.innerHTML = "Error loading WASM: " + err;
    console.error(err);
}
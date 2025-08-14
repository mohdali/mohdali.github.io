import { chromium } from 'playwright';
import { createServer } from 'http';
import { createReadStream, existsSync, readFileSync, writeFileSync, mkdirSync, readdirSync, statSync } from 'fs';
import { join, dirname, extname, basename } from 'path';
import { fileURLToPath } from 'url';
import { gzipSync } from 'zlib';

const __dirname = dirname(fileURLToPath(import.meta.url));
const wwwroot = join(__dirname, 'output', 'wwwroot');
const port = 5000;

// Mime types
const mimeTypes = {
  '.html': 'text/html',
  '.js': 'application/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.wasm': 'application/wasm',
  '.svg': 'image/svg+xml',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.ttf': 'font/ttf',
  '.eot': 'application/vnd.ms-fontobject'
};

// Create a simple static file server
const server = createServer((req, res) => {
  let filePath = join(wwwroot, req.url === '/' ? 'index.html' : req.url);
  
  // Check for compressed versions first
  if (existsSync(filePath + '.gz')) {
    filePath = filePath + '.gz';
    res.setHeader('Content-Encoding', 'gzip');
  } else if (existsSync(filePath + '.br')) {
    filePath = filePath + '.br';
    res.setHeader('Content-Encoding', 'br');
  } else if (!existsSync(filePath)) {
    // Fallback to index.html for SPA routes
    filePath = join(wwwroot, 'index.html');
    if (existsSync(filePath + '.gz')) {
      filePath = filePath + '.gz';
      res.setHeader('Content-Encoding', 'gzip');
    }
  }
  
  if (existsSync(filePath)) {
    const ext = extname(filePath.replace(/\.(gz|br)$/, ''));
    res.setHeader('Content-Type', mimeTypes[ext] || 'application/octet-stream');
    createReadStream(filePath).pipe(res);
  } else {
    res.statusCode = 404;
    res.end('Not found');
  }
});

async function prerender() {
  console.log('🚀 Starting pre-rendering process...');
  
  // Start the server
  await new Promise(resolve => server.listen(port, resolve));
  console.log(`📡 Server running at http://localhost:${port}`);
  
  // Launch Playwright
  console.log('🌐 Launching browser...');
  const browser = await chromium.launch({
    headless: true
  });
  
  try {
    const context = await browser.newContext({
      viewport: { width: 1280, height: 720 }
    });
    const page = await context.newPage();
    
    // Dynamically discover blog post routes
    const routes = ['/', '/posts', '/about'];
    
    // Find all blog post razor files
    const postsDir = join(__dirname, '..', 'src', 'mohdali.github.io', 'Pages', 'Posts');
    
    function findRazorFiles(dir) {
      const files = [];
      const items = readdirSync(dir);
      
      for (const item of items) {
        const fullPath = join(dir, item);
        const stat = statSync(fullPath);
        
        if (stat.isDirectory()) {
          files.push(...findRazorFiles(fullPath));
        } else if (item.endsWith('.razor') && !item.startsWith('_')) {
          // Read the file to extract the @page directive
          const content = readFileSync(fullPath, 'utf8');
          const pageMatch = content.match(/@page\s+"([^"]+)"/);
          if (pageMatch) {
            routes.push(pageMatch[1]);
            console.log(`📝 Found route: ${pageMatch[1]}`);
          }
        }
      }
      return files;
    }
    
    try {
      findRazorFiles(postsDir);
    } catch (error) {
      console.log('⚠️  Could not auto-discover routes:', error.message);
    }
    
    console.log(`🔍 Total routes to pre-render: ${routes.length}`);
    
    for (const route of routes) {
      console.log(`📄 Pre-rendering ${route}...`);
      
      const url = `http://localhost:${port}${route}`;
      await page.goto(url, {
        waitUntil: 'networkidle0',
        timeout: 30000
      });
      
      // Wait for Blazor to fully load
      await page.waitForFunction(
        () => {
          // Check if Blazor has loaded by looking for the removal of "Loading..." text
          const app = document.querySelector('#app');
          return app && !app.textContent.includes('Loading...');
        },
        { timeout: 15000 }
      ).catch(() => {
        console.log(`⚠️  Timeout waiting for Blazor to load on ${route}, continuing...`);
      });
      
      // Get the rendered HTML
      const html = await page.content();
      
      // Determine output path
      let outputPath;
      if (route === '/') {
        outputPath = join(wwwroot, 'index.html');
      } else {
        const routeDir = join(wwwroot, route);
        mkdirSync(routeDir, { recursive: true });
        outputPath = join(routeDir, 'index.html');
      }
      
      // Write the pre-rendered HTML
      writeFileSync(outputPath, html);
      console.log(`✅ Saved ${outputPath}`);
    }
    
    // Create 404.html for GitHub Pages
    const indexHtml = readFileSync(join(wwwroot, 'index.html'), 'utf8');
    writeFileSync(join(wwwroot, '404.html'), indexHtml);
    console.log('✅ Created 404.html for GitHub Pages');
    
  } finally {
    await browser.close();
    server.close();
    console.log('🎉 Pre-rendering complete!');
  }
}

// Run the pre-rendering
prerender().catch(error => {
  console.error('❌ Pre-rendering failed:', error);
  process.exit(1);
});
import base64
import os

with open('photo_placeholder.png', 'rb') as f:
    b64_avatar = base64.b64encode(f.read()).decode('utf-8')

html_content = f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>1-Slide Resume PPT Template Preview</title>
    <script src="https://www.gstatic.com/antigravity/web/dev/tailwindcss.min.js"></script>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            background-color: #0b0f19;
            color: #f1f5f9;
        }}
        .aspect-slide {{
            aspect-ratio: 16 / 9;
            width: 100%;
            max-width: 1100px;
        }}
    </style>
</head>
<body class="p-4 md:p-8 flex flex-col items-center min-h-screen">

    <!-- Header bar -->
    <div class="max-w-5xl w-full flex flex-wrap items-center justify-between mb-6 pb-4 border-b border-slate-800 gap-4">
        <div>
            <h1 class="text-2xl font-bold text-white flex items-center gap-2">
                <span class="p-1.5 rounded-lg bg-blue-600 text-white text-base">📄</span>
                1-Slide Resume PowerPoint Template
            </h1>
            <p class="text-xs text-slate-400 mt-1">
                Generated PowerPoint file: <code class="text-sky-400 bg-slate-900 px-1.5 py-0.5 rounded font-mono">Resume_Template_1Slide.pptx</code>
            </p>
        </div>
        <div class="flex items-center gap-2">
            <button onclick="switchSlide(1)" id="btn-s1" class="px-4 py-2 text-xs font-semibold rounded-lg bg-blue-600 text-white transition hover:bg-blue-500 shadow-sm">
                Slide 1: Executive Sidebar
            </button>
            <button onclick="switchSlide(2)" id="btn-s2" class="px-4 py-2 text-xs font-semibold rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 transition">
                Slide 2: Top-Banner 2-Col
            </button>
        </div>
    </div>

    <!-- SLIDE 1 CONTAINER -->
    <div id="slide-1" class="aspect-slide bg-slate-100 rounded-xl overflow-hidden shadow-2xl border border-slate-700/50 p-3 md:p-4 text-slate-900 grid grid-cols-12 gap-3 md:gap-4 relative select-none">
        
        <!-- Left Sidebar (Navy) -->
        <div class="col-span-4 bg-slate-900 text-slate-100 rounded-xl p-3 md:p-4 flex flex-col justify-between shadow-md border border-slate-800">
            <!-- Avatar & Replacement prompt -->
            <div class="flex flex-col items-center text-center">
                <div class="relative group w-24 h-24 md:w-28 md:h-28 rounded-full overflow-hidden border-2 border-slate-700 shadow-md bg-slate-800 flex items-center justify-center">
                    <img src="data:image/png;base64,{b64_avatar}" alt="Avatar" class="w-full h-full object-cover" />
                </div>
                <span class="text-[10px] text-sky-400 mt-1 font-semibold uppercase tracking-wider">Photo Placeholder</span>
            </div>

            <!-- Contact -->
            <div class="mt-3 pt-2 border-t border-slate-800">
                <h3 class="text-[11px] font-bold text-sky-400 tracking-wider uppercase mb-1.5">Contact Details</h3>
                <ul class="space-y-1 text-[10px] text-slate-300">
                    <li class="flex items-center gap-1.5"><span>📞</span> +1 (555) 234-5678</li>
                    <li class="flex items-center gap-1.5"><span>✉️</span> alex.morgan@email.com</li>
                    <li class="flex items-center gap-1.5"><span>📍</span> San Francisco, CA (Hybrid/Remote)</li>
                    <li class="flex items-center gap-1.5"><span>🔗</span> linkedin.com/in/alex-morgan-dev</li>
                    <li class="flex items-center gap-1.5"><span>💻</span> github.com/alexmorgan-dev</li>
                </ul>
            </div>

            <!-- Skills -->
            <div class="mt-3 pt-2 border-t border-slate-800">
                <h3 class="text-[11px] font-bold text-sky-400 tracking-wider uppercase mb-1.5">Handful of Skills</h3>
                <div class="space-y-1 text-[9.5px] leading-tight">
                    <div><span class="text-white font-semibold">• Languages:</span> <span class="text-slate-300">Python, JS, TypeScript, SQL</span></div>
                    <div><span class="text-white font-semibold">• Frameworks:</span> <span class="text-slate-300">React, Node.js, FastAPI, Next.js</span></div>
                    <div><span class="text-white font-semibold">• Cloud & DB:</span> <span class="text-slate-300">AWS, PostgreSQL, Redis, Docker</span></div>
                    <div><span class="text-white font-semibold">• Practices:</span> <span class="text-slate-300">CI/CD, Agile/Scrum, REST, Git</span></div>
                </div>
            </div>

            <!-- Education -->
            <div class="mt-3 pt-2 border-t border-slate-800">
                <h3 class="text-[11px] font-bold text-sky-400 tracking-wider uppercase mb-1">Education Background</h3>
                <div class="text-[9.5px] text-slate-300 leading-tight">
                    <p class="font-bold text-white">B.S. in Computer Science</p>
                    <p class="text-slate-400">State University of Tech (2016 – 2020)</p>
                    <p class="text-slate-400">GPA: 3.8 / 4.0 | Magna Cum Laude</p>
                </div>
                <div class="mt-1 text-[9px] text-sky-300 font-medium">
                    Cert: AWS Solutions Architect, PSM I
                </div>
            </div>
        </div>

        <!-- Right Main Body -->
        <div class="col-span-8 flex flex-col gap-3 justify-between">
            <!-- Top Header Card -->
            <div class="bg-white rounded-xl p-3 md:p-3.5 shadow-sm border border-slate-200">
                <div class="flex justify-between items-baseline">
                    <h2 class="text-xl md:text-2xl font-black text-slate-900 tracking-tight">ALEXANDER MORGAN</h2>
                    <span class="text-[9px] font-bold px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
                        4 YEARS TOTAL EXPERIENCE
                    </span>
                </div>
                <p class="text-xs font-bold text-blue-600 mt-0.5">SENIOR SOFTWARE ENGINEER</p>
                <p class="text-[10px] text-slate-600 mt-1 leading-snug">
                    Full-Stack Software Engineer with 4 years of proven track record leading end-to-end development across 2 enterprise-scale projects. Expert in cloud-native microservices, modern front-end architectures, high-performance database optimization, and cross-functional agile leadership.
                </p>
            </div>

            <!-- Experience Card (4 YOE in 2 Projects) -->
            <div class="bg-white rounded-xl p-3 md:p-3.5 shadow-sm border border-slate-200 flex-1 flex flex-col justify-between">
                <div>
                    <h3 class="text-xs font-extrabold text-slate-900 uppercase tracking-wide border-b border-slate-200 pb-1 mb-2 flex items-center justify-between">
                        <span>Work Experience (4 Years Total — 2 Key Projects)</span>
                        <span class="text-[9px] text-slate-500 font-semibold">2020 – Present</span>
                    </h3>

                    <!-- Project 1 -->
                    <div class="mb-2.5">
                        <div class="flex justify-between items-baseline">
                            <h4 class="text-xs font-bold text-blue-600">Project 1: Enterprise Cloud Telemetry & Analytics Platform</h4>
                            <span class="text-[9px] font-semibold text-slate-500">2.5 Years (Jan 2022 – Present)</span>
                        </div>
                        <p class="text-[10px] font-semibold text-slate-800">Apex Global Technologies | Role: Lead Full-Stack Developer</p>
                        <p class="text-[9px] italic text-slate-500 mb-1">Overview: Engineered high-throughput B2B monitoring suite processing telemetry for 150+ enterprise clients.</p>
                        <ul class="space-y-0.5 text-[9.5px] text-slate-700 list-disc list-inside leading-tight">
                            <li>Architected Python (FastAPI) & PostgreSQL microservices processing 300,000+ daily events with 99.98% uptime.</li>
                            <li>Engineered real-time React/TypeScript dashboard with WebSockets, cutting page render latency by 42%.</li>
                            <li>Automated AWS (ECS, RDS, S3) CI/CD deployment pipelines, reducing release cycles from 2 weeks to 2 days.</li>
                            <li>Implemented Redis query caching, reducing average API response times from 350ms to 78ms.</li>
                        </ul>
                        <div class="mt-1 text-[8.5px] font-semibold text-blue-700 bg-blue-50 px-2 py-0.5 rounded border border-blue-100 inline-block">
                            ⚡ Tech: Python, FastAPI, React, TypeScript, PostgreSQL, Redis, Docker, AWS ECS, Git
                        </div>
                    </div>

                    <!-- Divider -->
                    <div class="border-t border-dashed border-slate-200 my-2"></div>

                    <!-- Project 2 -->
                    <div>
                        <div class="flex justify-between items-baseline">
                            <h4 class="text-xs font-bold text-teal-700">Project 2: NextGen Digital Payment Gateway & Merchant Portal</h4>
                            <span class="text-[9px] font-semibold text-slate-500">1.5 Years (Jun 2020 – Dec 2021)</span>
                        </div>
                        <p class="text-[10px] font-semibold text-slate-800">FinSecure Labs | Role: Software Engineer</p>
                        <p class="text-[9px] italic text-slate-500 mb-1">Overview: Developed secure payment checkout workflows and merchant portal handling $20M+ annual transaction volume.</p>
                        <ul class="space-y-0.5 text-[9.5px] text-slate-700 list-disc list-inside leading-tight">
                            <li>Built resilient REST APIs in Node.js/Express, integrating Stripe and PayPal webhooks with idempotency guarantees.</li>
                            <li>Created responsive React/Redux merchant portal, reducing customer onboarding drop-off by 24%.</li>
                            <li>Authored automated unit and integration tests using Jest/Supertest, elevating test coverage from 60% to 91%.</li>
                            <li>Achieved PCI-DSS Level 1 compliance through tokenization and end-to-end SSL/TLS encryption.</li>
                        </ul>
                        <div class="mt-1 text-[8.5px] font-semibold text-teal-800 bg-teal-50 px-2 py-0.5 rounded border border-teal-100 inline-block">
                            ⚡ Tech: Node.js, Express, React, Redux, MongoDB, Jest, REST APIs, Stripe SDK, Docker
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- SLIDE 2 CONTAINER (Hidden by default) -->
    <div id="slide-2" class="aspect-slide bg-slate-100 rounded-xl overflow-hidden shadow-2xl border border-slate-700/50 p-3 md:p-4 text-slate-900 hidden flex-col gap-3 select-none">
        <!-- Top Banner -->
        <div class="bg-slate-900 text-white rounded-xl p-3 md:p-3.5 flex items-center gap-3 border border-slate-800 shadow-md">
            <div class="w-16 h-16 md:w-20 md:h-20 rounded-full overflow-hidden border-2 border-slate-600 bg-slate-800 shrink-0">
                <img src="data:image/png;base64,{b64_avatar}" alt="Avatar" class="w-full h-full object-cover" />
            </div>
            <div class="flex-1">
                <div class="flex justify-between items-baseline">
                    <h2 class="text-xl md:text-2xl font-black text-white tracking-tight">
                        ALEXANDER MORGAN <span class="text-xs font-normal text-sky-400 ml-2">| SENIOR SOFTWARE ENGINEER</span>
                    </h2>
                    <span class="text-[9px] font-bold px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                        4 YEARS EXPERIENCE
                    </span>
                </div>
                <div class="text-[9.5px] text-slate-300 flex flex-wrap gap-x-3 gap-y-0.5 mt-0.5">
                    <span>📞 +1 (555) 234-5678</span>
                    <span>✉️ alex.morgan@email.com</span>
                    <span>📍 San Francisco, CA</span>
                    <span>🔗 linkedin.com/in/alex-morgan</span>
                    <span>💻 github.com/alexmorgan</span>
                </div>
                <p class="text-[9px] text-slate-400 mt-1 line-clamp-2 leading-relaxed">
                    High-performing engineer with 4 years of experience delivering full-stack solutions across 2 major client projects. Proven strengths in system design, cloud automation, API development, and agile team delivery.
                </p>
            </div>
        </div>

        <!-- 2 Columns Below -->
        <div class="grid grid-cols-12 gap-3 flex-1">
            <!-- Left Col: 4 YOE Projects (8 cols) -->
            <div class="col-span-8 bg-white rounded-xl p-3 shadow-sm border border-slate-200 flex flex-col justify-between">
                <h3 class="text-xs font-extrabold text-slate-900 uppercase tracking-wide border-b border-slate-200 pb-1 mb-2">
                    Project Experience (4 Years Total in 2 Projects)
                </h3>
                
                <!-- P1 -->
                <div class="mb-2">
                    <div class="flex justify-between items-baseline">
                        <h4 class="text-[11.5px] font-bold text-blue-600">Project 1: Cloud Telemetry & Analytics Platform (2.5 Yrs)</h4>
                        <span class="text-[9px] font-semibold text-slate-500">2022 – Present</span>
                    </div>
                    <p class="text-[9.5px] font-semibold text-slate-800">Apex Global Technologies  •  Lead Full-Stack Developer</p>
                    <ul class="space-y-0.5 text-[9px] text-slate-700 list-disc list-inside mt-0.5 leading-tight">
                        <li>Architected Python (FastAPI) & PostgreSQL microservices for 300k+ daily events (99.98% uptime).</li>
                        <li>Built real-time React/TypeScript dashboard with WebSockets, slashing latency by 42%.</li>
                        <li>Automated AWS ECS/RDS CI/CD workflows, shortening deployment sprints from 2 weeks to 2 days.</li>
                        <li>Implemented Redis caching to accelerate API queries from 350ms to 78ms.</li>
                    </ul>
                    <span class="text-[8px] font-semibold text-blue-700 bg-blue-50 px-1.5 py-0.5 rounded border border-blue-100 inline-block mt-1">
                        Tech: Python, FastAPI, React, TypeScript, PostgreSQL, Redis, Docker, AWS ECS
                    </span>
                </div>

                <div class="border-t border-slate-200 my-1"></div>

                <!-- P2 -->
                <div>
                    <div class="flex justify-between items-baseline">
                        <h4 class="text-[11.5px] font-bold text-teal-700">Project 2: NextGen Digital Payment Gateway & Portal (1.5 Yrs)</h4>
                        <span class="text-[9px] font-semibold text-slate-500">2020 – 2021</span>
                    </div>
                    <p class="text-[9.5px] font-semibold text-slate-800">FinSecure Labs  •  Software Engineer</p>
                    <ul class="space-y-0.5 text-[9px] text-slate-700 list-disc list-inside mt-0.5 leading-tight">
                        <li>Integrated Stripe/PayPal checkout APIs with webhook idempotency handling $20M+ volume.</li>
                        <li>Engineered React/Redux onboarding portal, diminishing KYC signup friction by 24%.</li>
                        <li>Wrote comprehensive Jest test suite, increasing overall unit code coverage from 60% to 91%.</li>
                        <li>Delivered PCI-DSS compliant architecture via tokenization and TLS encryption.</li>
                    </ul>
                    <span class="text-[8px] font-semibold text-teal-800 bg-teal-50 px-1.5 py-0.5 rounded border border-teal-100 inline-block mt-1">
                        Tech: Node.js, Express, React, Redux, MongoDB, Jest, Stripe SDK, Docker
                    </span>
                </div>
            </div>

            <!-- Right Col: Skills & Education (4 cols) -->
            <div class="col-span-4 bg-white rounded-xl p-3 shadow-sm border border-slate-200 flex flex-col justify-between">
                <div>
                    <h3 class="text-xs font-extrabold text-slate-900 uppercase tracking-wide border-b border-slate-200 pb-1 mb-2">
                        Handful of Skills
                    </h3>
                    <div class="space-y-1.5 text-[9px]">
                        <div>
                            <span class="font-bold text-blue-600 block">• Languages:</span>
                            <span class="text-slate-600">Python, JavaScript, TypeScript, SQL</span>
                        </div>
                        <div>
                            <span class="font-bold text-blue-600 block">• Frameworks:</span>
                            <span class="text-slate-600">React, Node.js, FastAPI, Next.js</span>
                        </div>
                        <div>
                            <span class="font-bold text-blue-600 block">• Cloud & Databases:</span>
                            <span class="text-slate-600">AWS, PostgreSQL, Redis, Docker</span>
                        </div>
                        <div>
                            <span class="font-bold text-blue-600 block">• Tools & Methods:</span>
                            <span class="text-slate-600">Git, CI/CD, Agile/Scrum, REST APIs</span>
                        </div>
                    </div>
                </div>

                <div class="border-t border-slate-200 pt-2">
                    <h3 class="text-xs font-extrabold text-slate-900 uppercase tracking-wide mb-1">
                        Education Background
                    </h3>
                    <p class="text-[9px] font-bold text-slate-800">B.S. in Computer Science</p>
                    <p class="text-[8.5px] text-slate-500">State University of Tech (2016 – 2020)</p>
                    <p class="text-[8px] text-slate-500">GPA: 3.8 / 4.0 | Magna Cum Laude</p>
                    <p class="text-[8px] text-teal-700 font-semibold mt-1">AWS Solutions Architect, PSM I</p>
                </div>

                <div class="border-t border-slate-200 pt-2">
                    <h3 class="text-xs font-extrabold text-slate-900 uppercase tracking-wide mb-1">
                        Key Highlights
                    </h3>
                    <div class="text-[8px] text-slate-600 space-y-0.5">
                        <p>⭐ 4+ Years Enterprise Engineering Exp.</p>
                        <p>⭐ 2 Successful Projects Shipped to Prod</p>
                        <p>⭐ Mentored 4 Junior Engineers & Interns</p>
                        <p>⭐ 99.9% Production SLA Record</p>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Quick instructions footer -->
    <div class="max-w-5xl w-full mt-6 bg-slate-900/80 border border-slate-800 rounded-xl p-4 text-xs text-slate-400 flex flex-col md:flex-row justify-between items-center gap-4">
        <div>
            <span class="font-bold text-white">How to edit in PowerPoint:</span>
            <ul class="list-disc list-inside mt-1 space-y-0.5 text-slate-300">
                <li><b>Replace Photo:</b> Right-click the photo placeholder &rarr; <i>Change Picture &rarr; This Device...</i></li>
                <li><b>Edit Text:</b> Double-click any text box to replace names, metrics, skills, and dates with your own.</li>
                <li><b>Save as PDF:</b> In PowerPoint, go to <i>File &rarr; Export &rarr; Create PDF/XPS</i> for submitting to recruiters!</li>
            </ul>
        </div>
    </div>

    <script>
        function switchSlide(num) {{
            const s1 = document.getElementById('slide-1');
            const s2 = document.getElementById('slide-2');
            const b1 = document.getElementById('btn-s1');
            const b2 = document.getElementById('btn-s2');

            if (num === 1) {{
                s1.classList.remove('hidden');
                s1.classList.add('grid');
                s2.classList.add('hidden');
                s2.classList.remove('flex');
                b1.className = "px-4 py-2 text-xs font-semibold rounded-lg bg-blue-600 text-white transition hover:bg-blue-500 shadow-sm";
                b2.className = "px-4 py-2 text-xs font-semibold rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 transition";
            }} else {{
                s2.classList.remove('hidden');
                s2.classList.add('flex');
                s1.classList.add('hidden');
                s1.classList.remove('grid');
                b2.className = "px-4 py-2 text-xs font-semibold rounded-lg bg-blue-600 text-white transition hover:bg-blue-500 shadow-sm";
                b1.className = "px-4 py-2 text-xs font-semibold rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 transition";
            }}
        }}
    </script>
</body>
</html>
"""

# Write to workspace
with open("Resume_Preview.html", "w", encoding="utf-8") as f:
    f.write(html_content)

# Also write to artifact folder
artifact_dir = r"C:\Users\Asus\.gemini\antigravity\brain\8e90f162-5883-4ccc-9866-b8740bbdbabc"
if os.path.exists(artifact_dir):
    with open(os.path.join(artifact_dir, "resume_preview.html"), "w", encoding="utf-8") as f:
        f.write(html_content)

print("Generated Resume_Preview.html successfully in workspace and artifact directory.")

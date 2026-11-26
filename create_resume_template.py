import os
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from PIL import Image, ImageDraw, ImageFont

def create_placeholder_image(output_path):
    width, height = 500, 500
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Outer circle border
    draw.ellipse((10, 10, 490, 490), fill=(241, 245, 249, 255), outline=(148, 163, 184, 255), width=6)
    
    # Head & Shoulders silhouette
    draw.ellipse((190, 110, 310, 230), fill=(100, 116, 139, 255))
    draw.chord((120, 250, 380, 440), start=0, end=180, fill=(100, 116, 139, 255))
    
    # Edit / Replace badge
    pill = (110, 410, 390, 470)
    draw.rounded_rectangle(pill, radius=20, fill=(37, 99, 235, 255))
    
    try:
        font = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 24)
    except:
        font = ImageFont.load_default()
        
    draw.text((250, 440), "REPLACE PHOTO", fill=(255, 255, 255, 255), font=font, anchor="mm")
    img.save(output_path, 'PNG')
    print(f"Saved avatar placeholder: {output_path}")

def add_header_text(tf, text, size_pt, bold=True, color_rgb=(15, 23, 42)):
    p = tf.paragraphs[0] if tf.paragraphs else tf.add_paragraph()
    p.text = text
    p.font.name = 'Segoe UI'
    p.font.size = Pt(size_pt)
    p.font.bold = bold
    p.font.color.rgb = RGBColor(*color_rgb)
    return p

def add_paragraph(tf, text, size_pt, bold=False, color_rgb=(71, 85, 105), space_after=2):
    p = tf.add_paragraph()
    p.text = text
    p.font.name = 'Segoe UI'
    p.font.size = Pt(size_pt)
    p.font.bold = bold
    p.font.color.rgb = RGBColor(*color_rgb)
    p.space_after = Pt(space_after)
    return p

def set_shape_flat(shape, fill_rgb, border_rgb=None, border_width=1):
    shape.fill.solid()
    shape.fill.fore_color.rgb = RGBColor(*fill_rgb)
    if border_rgb:
        shape.line.color.rgb = RGBColor(*border_rgb)
        shape.line.width = Pt(border_width)
    else:
        shape.line.fill.background()

def build_presentation(output_pptx="Resume_Template_1Slide.pptx"):
    prs = Presentation()
    prs.slide_width = Inches(13.333)
    prs.slide_height = Inches(7.5)
    blank_layout = prs.slide_layouts[6]
    
    img_path = os.path.abspath("photo_placeholder.png")
    if not os.path.exists(img_path):
        create_placeholder_image(img_path)
        
    # ==========================================
    # SLIDE 1: MODERN EXECUTIVE SIDEBAR LAYOUT
    # ==========================================
    slide1 = prs.slides.add_slide(blank_layout)
    
    # 1. Slide Background
    bg1 = slide1.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, Inches(13.333), Inches(7.5))
    set_shape_flat(bg1, (248, 250, 252)) # Light slate bg
    
    # 2. Left Sidebar (Dark Navy Card)
    sb_w, sb_h = Inches(3.4), Inches(6.7)
    sb_left, sb_top = Inches(0.4), Inches(0.4)
    sidebar = slide1.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, sb_left, sb_top, sb_w, sb_h)
    set_shape_flat(sidebar, (15, 23, 42), (30, 41, 59), 1) # Dark navy (#0F172A)
    
    # 3. Photo Placeholder in Sidebar
    photo_size = Inches(1.8)
    photo_left = sb_left + (sb_w - photo_size) / 2
    photo_top = Inches(0.65)
    slide1.shapes.add_picture(img_path, photo_left, photo_top, photo_size, photo_size)
    
    # 4. Sidebar Content (Contact, Skills, Education)
    sb_box = slide1.shapes.add_textbox(sb_left + Inches(0.25), photo_top + photo_size + Inches(0.12), sb_w - Inches(0.5), Inches(4.0))
    tf1 = sb_box.text_frame
    tf1.word_wrap = True
    tf1.margin_left = tf1.margin_right = tf1.margin_top = tf1.margin_bottom = 0
    
    # Contact Section
    p = tf1.paragraphs[0]
    p.text = "CONTACT DETAILS"
    p.font.name = 'Segoe UI'
    p.font.size = Pt(10.5)
    p.font.bold = True
    p.font.color.rgb = RGBColor(56, 189, 248) # Cyan accent
    p.space_after = Pt(4)
    
    contacts = [
        "📞  +1 (555) 234-5678",
        "✉️  alex.morgan@email.com",
        "📍  San Francisco, CA (Hybrid / Remote)",
        "🔗  linkedin.com/in/alex-morgan-dev",
        "💻  github.com/alexmorgan-dev"
    ]
    for c in contacts:
        add_paragraph(tf1, c, 8.5, False, (226, 232, 240), space_after=2)
        
    # Skills Section
    p_sk = add_paragraph(tf1, "HANDFUL OF SKILLS", 10.5, True, (56, 189, 248), space_after=4)
    p_sk.space_before = Pt(8)
    
    skills = [
        ("Core Languages:", "Python, JavaScript, TypeScript, SQL"),
        ("Frameworks:", "React, Node.js, FastAPI, Next.js"),
        ("Cloud & DB:", "AWS, PostgreSQL, Redis, Docker"),
        ("Methodologies:", "CI/CD, Agile/Scrum, REST APIs, Git")
    ]
    for cat, items in skills:
        p = tf1.add_paragraph()
        r1 = p.add_run()
        r1.text = f"• {cat} "
        r1.font.bold = True
        r1.font.size = Pt(8.5)
        r1.font.name = 'Segoe UI'
        r1.font.color.rgb = RGBColor(255, 255, 255)
        
        r2 = p.add_run()
        r2.text = items
        r2.font.bold = False
        r2.font.size = Pt(8.5)
        r2.font.name = 'Segoe UI'
        r2.font.color.rgb = RGBColor(203, 213, 225)
        p.space_after = Pt(2)
        
    # Education Section
    p_ed = add_paragraph(tf1, "EDUCATION BACKGROUND", 10.5, True, (56, 189, 248), space_after=4)
    p_ed.space_before = Pt(8)
    
    p = tf1.add_paragraph()
    r = p.add_run()
    r.text = "B.S. in Computer Science\n"
    r.font.bold = True
    r.font.size = Pt(8.5)
    r.font.color.rgb = RGBColor(255, 255, 255)
    
    r_sub = p.add_run()
    r_sub.text = "State University of Technology (2016 – 2020)\nGPA: 3.8 / 4.0 | Magna Cum Laude"
    r_sub.font.size = Pt(8)
    r_sub.font.color.rgb = RGBColor(203, 213, 225)
    p.space_after = Pt(4)
    
    p_cert = tf1.add_paragraph()
    r_cert = p_cert.add_run()
    r_cert.text = "Certifications:\n"
    r_cert.font.bold = True
    r_cert.font.size = Pt(8.5)
    r_cert.font.color.rgb = RGBColor(255, 255, 255)
    
    r_cnames = p_cert.add_run()
    r_cnames.text = "• AWS Certified Solutions Architect\n• Professional Scrum Master I (PSM I)"
    r_cnames.font.size = Pt(8)
    r_cnames.font.color.rgb = RGBColor(56, 189, 248)
    
    # 5. Right Area: Top Header Card
    main_left = Inches(4.0)
    main_w = Inches(8.933)
    header_card = slide1.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, main_left, Inches(0.4), main_w, Inches(1.35))
    set_shape_flat(header_card, (255, 255, 255), (226, 232, 240), 1)
    
    tb_header = slide1.shapes.add_textbox(main_left + Inches(0.3), Inches(0.45), main_w - Inches(0.6), Inches(1.25))
    tf_h = tb_header.text_frame
    tf_h.word_wrap = True
    tf_h.margin_left = tf_h.margin_right = tf_h.margin_top = tf_h.margin_bottom = 0
    
    # Name
    p_name = tf_h.paragraphs[0]
    p_name.text = "ALEXANDER MORGAN"
    p_name.font.name = 'Segoe UI'
    p_name.font.size = Pt(22)
    p_name.font.bold = True
    p_name.font.color.rgb = RGBColor(15, 23, 42)
    
    # Role Title with 4 YOE badge
    p_role = tf_h.add_paragraph()
    r_role = p_role.add_run()
    r_role.text = "SENIOR SOFTWARE ENGINEER  "
    r_role.font.bold = True
    r_role.font.size = Pt(11)
    r_role.font.color.rgb = RGBColor(37, 99, 235) # Blue
    
    r_exp = p_role.add_run()
    r_exp.text = "•  4 YEARS PROFESSIONAL EXPERIENCE ACROSS 2 CORE PROJECTS"
    r_exp.font.bold = True
    r_exp.font.size = Pt(10)
    r_exp.font.color.rgb = RGBColor(13, 148, 136) # Teal
    p_role.space_after = Pt(3)
    
    p_sum = tf_h.add_paragraph()
    p_sum.text = "Proactive and solution-driven Software Engineer with 4 years of hands-on experience designing, scaling, and deploying mission-critical web applications. Proven track record of delivering end-to-end features, optimizing distributed databases, and collaborating across high-performing agile squads."
    p_sum.font.size = Pt(8.5)
    p_sum.font.color.rgb = RGBColor(71, 85, 105)
    
    # 6. Right Area: Experience Card (4 Years in 2 Projects)
    exp_card = slide1.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, main_left, Inches(1.88), main_w, Inches(5.22))
    set_shape_flat(exp_card, (255, 255, 255), (226, 232, 240), 1)
    
    tb_exp = slide1.shapes.add_textbox(main_left + Inches(0.3), Inches(1.95), main_w - Inches(0.6), Inches(5.0))
    tf_e = tb_exp.text_frame
    tf_e.word_wrap = True
    tf_e.margin_left = tf_e.margin_right = tf_e.margin_top = tf_e.margin_bottom = 0
    
    # Experience Header
    p_exph = tf_e.paragraphs[0]
    p_exph.text = "WORK EXPERIENCE (4 YEARS TOTAL — 2 KEY PROJECTS)"
    p_exph.font.name = 'Segoe UI'
    p_exph.font.size = Pt(11.5)
    p_exph.font.bold = True
    p_exph.font.color.rgb = RGBColor(15, 23, 42)
    p_exph.space_after = Pt(4)
    
    # PROJECT 1 (2.5 Years)
    p_p1_title = tf_e.add_paragraph()
    r_p1 = p_p1_title.add_run()
    r_p1.text = "Project 1: Enterprise Cloud Telemetry & Analytics Platform"
    r_p1.font.bold = True
    r_p1.font.size = Pt(10.5)
    r_p1.font.color.rgb = RGBColor(37, 99, 235)
    
    p_p1_meta = tf_e.add_paragraph()
    r_m1 = p_p1_meta.add_run()
    r_m1.text = "Role: Lead Full-Stack Developer  |  Company: Apex Global Technologies  |  Duration: 2.5 Years (Jan 2022 – Present)"
    r_m1.font.bold = True
    r_m1.font.size = Pt(8.5)
    r_m1.font.color.rgb = RGBColor(15, 23, 42)
    
    p_p1_desc = tf_e.add_paragraph()
    p_p1_desc.text = "Overview: Engineered an end-to-end B2B data monitoring suite processing live analytics for over 150 enterprise clients."
    p_p1_desc.font.size = Pt(8)
    p_p1_desc.font.italic = True
    p_p1_desc.font.color.rgb = RGBColor(100, 116, 139)
    p_p1_desc.space_after = Pt(2)
    
    p1_bullets = [
        "Architected scalable microservices using Python (FastAPI) and PostgreSQL, handling 300,000+ daily telemetry events with 99.98% uptime.",
        "Engineered real-time interactive dashboards with React, TypeScript, and WebSockets, cutting page render latency by 42%.",
        "Containerized core services with Docker and automated CI/CD pipeline deployments to AWS (ECS, RDS, S3), trimming release cycle time from 2 weeks to 2 days.",
        "Spearheaded database indexing and Redis query caching, reducing average API response time from 350ms to 78ms."
    ]
    for b in p1_bullets:
        p = tf_e.add_paragraph()
        r = p.add_run()
        r.text = f"•  {b}"
        r.font.size = Pt(8)
        r.font.color.rgb = RGBColor(51, 65, 85)
        p.space_after = Pt(1)
        
    p1_tech = tf_e.add_paragraph()
    r_t1 = p1_tech.add_run()
    r_t1.text = "  ⚡ Technologies: Python, FastAPI, React, TypeScript, PostgreSQL, Redis, Docker, AWS ECS, Git  "
    r_t1.font.size = Pt(7.8)
    r_t1.font.bold = True
    r_t1.font.color.rgb = RGBColor(30, 64, 175)
    p1_tech.space_before = Pt(2)
    p1_tech.space_after = Pt(6)
    
    # PROJECT 2 (1.5 Years)
    p_p2_title = tf_e.add_paragraph()
    r_p2 = p_p2_title.add_run()
    r_p2.text = "Project 2: NextGen Digital Payment Gateway & Merchant Portal"
    r_p2.font.bold = True
    r_p2.font.size = Pt(10.5)
    r_p2.font.color.rgb = RGBColor(13, 148, 136) # Teal
    
    p_p2_meta = tf_e.add_paragraph()
    r_m2 = p_p2_meta.add_run()
    r_m2.text = "Role: Software Engineer  |  Company: FinSecure Labs  |  Duration: 1.5 Years (Jun 2020 – Dec 2021)"
    r_m2.font.bold = True
    r_m2.font.size = Pt(8.5)
    r_m2.font.color.rgb = RGBColor(15, 23, 42)
    
    p_p2_desc = tf_e.add_paragraph()
    p_p2_desc.text = "Overview: Developed secure payment checkout workflows and self-service merchant onboarding portal handling $20M+ annual volume."
    p_p2_desc.font.size = Pt(8)
    p_p2_desc.font.italic = True
    p_p2_desc.font.color.rgb = RGBColor(100, 116, 139)
    p_p2_desc.space_after = Pt(2)
    
    p2_bullets = [
        "Developed resilient REST APIs in Node.js/Express and integrated Stripe & PayPal webhooks with rigorous idempotency checks.",
        "Built responsive merchant onboarding portal in React/Redux, reducing new customer drop-off during KYC verification by 24%.",
        "Authored comprehensive automated unit and integration tests using Jest and Supertest, elevating test coverage from 60% to 91%.",
        "Collaborated with security teams to pass PCI-DSS Level 1 compliance audits, implementing tokenization and SSL/TLS encryption."
    ]
    for b in p2_bullets:
        p = tf_e.add_paragraph()
        r = p.add_run()
        r.text = f"•  {b}"
        r.font.size = Pt(8)
        r.font.color.rgb = RGBColor(51, 65, 85)
        p.space_after = Pt(1)
        
    p2_tech = tf_e.add_paragraph()
    r_t2 = p2_tech.add_run()
    r_t2.text = "  ⚡ Technologies: Node.js, Express, React, Redux, MongoDB, Jest, REST APIs, Stripe SDK, Docker  "
    r_t2.font.size = Pt(7.8)
    r_t2.font.bold = True
    r_t2.font.color.rgb = RGBColor(15, 118, 110)
    p2_tech.space_before = Pt(2)

    # ==========================================
    # SLIDE 2: MODERN CLEAN TOP-BANNER 2-COL LAYOUT
    # ==========================================
    slide2 = prs.slides.add_slide(blank_layout)
    
    # Slide 2 Background
    bg2 = slide2.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, Inches(13.333), Inches(7.5))
    set_shape_flat(bg2, (248, 250, 252))
    
    # Top Banner Header Card
    top_banner = slide2.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.4), Inches(0.4), Inches(12.533), Inches(1.5))
    set_shape_flat(top_banner, (15, 23, 42), (30, 41, 59), 1)
    
    # Photo in Top Left of Banner
    photo2_size = Inches(1.2)
    slide2.shapes.add_picture(img_path, Inches(0.55), Inches(0.55), photo2_size, photo2_size)
    
    # Banner Text Box
    tb_b = slide2.shapes.add_textbox(Inches(1.95), Inches(0.48), Inches(10.8), Inches(1.35))
    tf_b = tb_b.text_frame
    tf_b.word_wrap = True
    tf_b.margin_left = tf_b.margin_right = tf_b.margin_top = tf_b.margin_bottom = 0
    
    p_bname = tf_b.paragraphs[0]
    r_bn = p_bname.add_run()
    r_bn.text = "ALEXANDER MORGAN  "
    r_bn.font.bold = True
    r_bn.font.size = Pt(18)
    r_bn.font.color.rgb = RGBColor(255, 255, 255)
    
    r_btitle = p_bname.add_run()
    r_btitle.text = "|   SENIOR SOFTWARE ENGINEER (4 YEARS EXPERIENCE)"
    r_btitle.font.bold = True
    r_btitle.font.size = Pt(11)
    r_btitle.font.color.rgb = RGBColor(56, 189, 248)
    
    # Contact row
    p_bcontact = tf_b.add_paragraph()
    p_bcontact.text = "📞 +1 (555) 234-5678   •   ✉️ alex.morgan@email.com   •   📍 San Francisco, CA   •   🔗 linkedin.com/in/alex-morgan   •   💻 github.com/alexmorgan"
    p_bcontact.font.size = Pt(8.5)
    p_bcontact.font.color.rgb = RGBColor(203, 213, 225)
    p_bcontact.space_after = Pt(2)
    
    p_bsum = tf_b.add_paragraph()
    p_bsum.text = "High-performing engineer with 4 years of experience delivering full-stack solutions across 2 major client projects. Proven strengths in system design, cloud automation, API development, and agile team delivery."
    p_bsum.font.size = Pt(8)
    p_bsum.font.color.rgb = RGBColor(148, 163, 184)
    
    # 2-Column layout below
    col1_left = Inches(0.4)
    col1_w = Inches(8.3)
    col2_left = Inches(8.85)
    col2_w = Inches(4.083)
    body_top = Inches(2.02)
    body_h = Inches(5.08)
    
    # Col 1: Projects (Left Column)
    col1_card = slide2.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, col1_left, body_top, col1_w, body_h)
    set_shape_flat(col1_card, (255, 255, 255), (226, 232, 240), 1)
    
    tb_c1 = slide2.shapes.add_textbox(col1_left + Inches(0.25), body_top + Inches(0.12), col1_w - Inches(0.5), body_h - Inches(0.25))
    tf_c1 = tb_c1.text_frame
    tf_c1.word_wrap = True
    tf_c1.margin_left = tf_c1.margin_right = tf_c1.margin_top = tf_c1.margin_bottom = 0
    
    p_ch = tf_c1.paragraphs[0]
    p_ch.text = "PROJECT EXPERIENCE (4 YEARS IN 2 ENTERPRISE PROJECTS)"
    p_ch.font.name = 'Segoe UI'
    p_ch.font.size = Pt(11)
    p_ch.font.bold = True
    p_ch.font.color.rgb = RGBColor(15, 23, 42)
    p_ch.space_after = Pt(4)
    
    # P1
    p = tf_c1.add_paragraph()
    r = p.add_run()
    r.text = "Project 1: Cloud Telemetry & Analytics Platform (2.5 Years)"
    r.font.bold = True
    r.font.size = Pt(10)
    r.font.color.rgb = RGBColor(37, 99, 235)
    
    p = tf_c1.add_paragraph()
    r = p.add_run()
    r.text = "Apex Global Technologies  |  Lead Full-Stack Developer  |  Jan 2022 – Present"
    r.font.bold = True
    r.font.size = Pt(8.2)
    r.font.color.rgb = RGBColor(15, 23, 42)
    
    for b in p1_bullets:
        p = tf_c1.add_paragraph()
        r = p.add_run()
        r.text = f"• {b}"
        r.font.size = Pt(7.8)
        r.font.color.rgb = RGBColor(51, 65, 85)
        p.space_after = Pt(1)
        
    p = tf_c1.add_paragraph()
    r = p.add_run()
    r.text = "⚡ Stack: Python, FastAPI, React, TypeScript, PostgreSQL, Redis, AWS ECS, Docker"
    r.font.size = Pt(7.8)
    r.font.bold = True
    r.font.color.rgb = RGBColor(30, 64, 175)
    p.space_after = Pt(6)
    
    # P2
    p = tf_c1.add_paragraph()
    r = p.add_run()
    r.text = "Project 2: Digital Payment Gateway & Merchant Portal (1.5 Years)"
    r.font.bold = True
    r.font.size = Pt(10)
    r.font.color.rgb = RGBColor(13, 148, 136)
    
    p = tf_c1.add_paragraph()
    r = p.add_run()
    r.text = "FinSecure Labs  |  Software Engineer  |  Jun 2020 – Dec 2021"
    r.font.bold = True
    r.font.size = Pt(8.2)
    r.font.color.rgb = RGBColor(15, 23, 42)
    
    for b in p2_bullets:
        p = tf_c1.add_paragraph()
        r = p.add_run()
        r.text = f"• {b}"
        r.font.size = Pt(7.8)
        r.font.color.rgb = RGBColor(51, 65, 85)
        p.space_after = Pt(1)
        
    p = tf_c1.add_paragraph()
    r = p.add_run()
    r.text = "⚡ Stack: Node.js, Express, React, Redux, MongoDB, Jest, Stripe SDK, Docker"
    r.font.size = Pt(7.8)
    r.font.bold = True
    r.font.color.rgb = RGBColor(15, 118, 110)
    
    # Col 2: Skills & Education (Right Column)
    col2_card = slide2.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, col2_left, body_top, col2_w, body_h)
    set_shape_flat(col2_card, (255, 255, 255), (226, 232, 240), 1)
    
    tb_c2 = slide2.shapes.add_textbox(col2_left + Inches(0.25), body_top + Inches(0.12), col2_w - Inches(0.5), body_h - Inches(0.25))
    tf_c2 = tb_c2.text_frame
    tf_c2.word_wrap = True
    tf_c2.margin_left = tf_c2.margin_right = tf_c2.margin_top = tf_c2.margin_bottom = 0
    
    # Skills in Col 2
    p_skh = tf_c2.paragraphs[0]
    p_skh.text = "TECHNICAL SKILLS"
    p_skh.font.name = 'Segoe UI'
    p_skh.font.size = Pt(11)
    p_skh.font.bold = True
    p_skh.font.color.rgb = RGBColor(15, 23, 42)
    p_skh.space_after = Pt(4)
    
    for cat, items in skills:
        p = tf_c2.add_paragraph()
        r = p.add_run()
        r.text = f"• {cat}\n  "
        r.font.bold = True
        r.font.size = Pt(8.2)
        r.font.color.rgb = RGBColor(37, 99, 235)
        
        r2 = p.add_run()
        r2.text = items
        r2.font.bold = False
        r2.font.size = Pt(8.2)
        r2.font.color.rgb = RGBColor(71, 85, 105)
        p.space_after = Pt(2)
        
    # Education in Col 2
    p_edh = tf_c2.add_paragraph()
    p_edh.text = "EDUCATION BACKGROUND"
    p_edh.font.name = 'Segoe UI'
    p_edh.font.size = Pt(11)
    p_edh.font.bold = True
    p_edh.font.color.rgb = RGBColor(15, 23, 42)
    p_edh.space_before = Pt(8)
    p_edh.space_after = Pt(4)
    
    p = tf_c2.add_paragraph()
    r = p.add_run()
    r.text = "B.S. in Computer Science (2016 – 2020)\n"
    r.font.bold = True
    r.font.size = Pt(8.5)
    r.font.color.rgb = RGBColor(15, 23, 42)
    
    r2 = p.add_run()
    r2.text = "State University of Technology\nGPA: 3.8 / 4.0 | Magna Cum Laude"
    r2.font.size = Pt(8)
    r2.font.color.rgb = RGBColor(100, 116, 139)
    p.space_after = Pt(4)
    
    p = tf_c2.add_paragraph()
    r = p.add_run()
    r.text = "Certifications:\n"
    r.font.bold = True
    r.font.size = Pt(8.5)
    r.font.color.rgb = RGBColor(15, 23, 42)
    
    r2 = p.add_run()
    r2.text = "• AWS Certified Solutions Architect\n• Professional Scrum Master I (PSM I)"
    r2.font.size = Pt(8)
    r2.font.color.rgb = RGBColor(13, 148, 136)
    p.space_after = Pt(6)
    
    # Highlights Box
    p_hl = tf_c2.add_paragraph()
    p_hl.text = "KEY HIGHLIGHTS"
    p_hl.font.name = 'Segoe UI'
    p_hl.font.size = Pt(10.5)
    p_hl.font.bold = True
    p_hl.font.color.rgb = RGBColor(15, 23, 42)
    p_hl.space_before = Pt(6)
    p_hl.space_after = Pt(3)
    
    highlights = [
        "⭐ 4+ Years Enterprise Engineering Exp.",
        "⭐ 2 Successful Projects Shipped to Prod",
        "⭐ Mentored 4 Junior Engineers & Interns",
        "⭐ 99.9% Production SLA Record"
    ]
    for h in highlights:
        p = tf_c2.add_paragraph()
        r = p.add_run()
        r.text = h
        r.font.size = Pt(8)
        r.font.color.rgb = RGBColor(71, 85, 105)
        p.space_after = Pt(1)

    prs.save(output_pptx)
    print(f"Presentation successfully created at: {output_pptx}")

if __name__ == '__main__':
    build_presentation()

import os
import sys
import re
import base64
import html

sys.stdout.reconfigure(encoding='utf-8')

ROOT_DIR = r"d:\CD-CGTTU\SchedulingApp"
MD_PATH = os.path.join(ROOT_DIR, "BAO_CAO_NGHIEN_CUU_GREEDY_BASELINE.md")
DOC_PATH_1 = os.path.join(ROOT_DIR, "BAO_CAO_NGHIEN_CUU_GREEDY_BASELINE.doc")
DOC_PATH_2 = os.path.join(ROOT_DIR, "file báo cáo mẫu cá nhân .doc")

with open(MD_PATH, "r", encoding="utf-8") as f:
    md_content = f.read()

# Replace <img src="..."> with base64 embedded images
def replace_img(match):
    src = match.group(1).replace('/', '\\')
    full_path = os.path.join(ROOT_DIR, src)
    if not os.path.exists(full_path):
        return ""
    with open(full_path, "rb") as img_file:
        b64 = base64.b64encode(img_file.read()).decode("ascii")
    mime = "image/svg+xml" if src.endswith(".svg") else "image/png"
    return f'<div style="text-align:center;margin:20px 0;"><img src="data:{mime};base64,{b64}" style="max-width:850px;width:100%;display:block;margin:0 auto;border:1px solid #e2e8f0;border-radius:6px;box-shadow:0 2px 6px rgba(0,0,0,0.06);"/><br/><span style="font-size:9.5pt;color:#64748b;font-style:italic;">Hình. Biểu đồ đối sánh thực nghiệm điểm phạt của các biến thể Greedy (Mean of 30 replicates)</span></div>'

processed_md = re.sub(r'<img src="([^"]+)"[^>]*>', replace_img, md_content)

# Convert Markdown tables to HTML tables
def format_tables(text):
    lines = text.split('\n')
    in_table = False
    table_lines = []
    output_lines = []
    
    for line in lines:
        if line.strip().startswith('|') and line.strip().endswith('|'):
            if not in_table:
                in_table = True
                table_lines = []
            table_lines.append(line)
        else:
            if in_table:
                in_table = False
                output_lines.append(render_html_table(table_lines))
            output_lines.append(line)
            
    if in_table:
        output_lines.append(render_html_table(table_lines))
        
    return '\n'.join(output_lines)

def render_html_table(table_lines):
    if len(table_lines) < 2:
        return '\n'.join(table_lines)
    
    header_cells = [c.strip() for c in table_lines[0].strip('|').split('|')]
    rows_data = []
    for line in table_lines[2:]: # skip separator line
        cells = [c.strip() for c in line.strip('|').split('|')]
        rows_data.append(cells)
        
    html = ['<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:10pt;font-family:Calibri,Arial,sans-serif;">']
    html.append('<thead><tr style="background-color:#1e4d78;color:#ffffff;text-align:left;">')
    for cell in header_cells:
        html.append(f'<th style="border:1px solid #cbd5e1;padding:8px 12px;font-weight:600;">{cell}</th>')
    html.append('</tr></thead><tbody>')
    
    for r_idx, row in enumerate(rows_data):
        bg = "#f8fafc" if r_idx % 2 == 1 else "#ffffff"
        html.append(f'<tr style="background-color:{bg};">')
        for c_idx, cell in enumerate(row):
            align = "center" if c_idx > 1 and "%" in cell or cell.replace('.', '').isdigit() else "left"
            html.append(f'<td style="border:1px solid #cbd5e1;padding:7px 12px;text-align:{align};">{cell}</td>')
        html.append('</tr>')
        
    html.append('</tbody></table>')
    return ''.join(html)

processed_tables = format_tables(processed_md)

# Convert Markdown headings and formatting to HTML
def md_to_html(text):
    # Code blocks
    def code_block(match):
        code_content = html.escape(match.group(2).strip())
        return f'<pre style="background:#0f172a;color:#f8fafc;padding:14px;border-radius:6px;font-family:Consolas,monospace;font-size:9.5pt;line-height:1.45;overflow-x:auto;margin:14px 0;"><code>{code_content}</code></pre>'
    
    text = re.sub(r'```(\w+)?\n([\s\S]*?)```', code_block, text)
    
    # Inline code
    text = re.sub(r'`([^`]+)`', r'<code style="background:#e2e8f0;color:#0f172a;padding:2px 5px;border-radius:4px;font-family:Consolas,monospace;font-size:9.5pt;">\1</code>', text)
    
    # Headings
    text = re.sub(r'(?m)^#{1}\s+(.+)$', r'<h1 style="color:#123a63;border-bottom:2.5px solid #2563eb;padding-bottom:8px;margin-top:32px;font-size:18pt;font-family:Segoe UI,Calibri,sans-serif;">\1</h1>', text)
    text = re.sub(r'(?m)^#{2}\s+(.+)$', r'<h2 style="color:#1e4d78;border-bottom:1px solid #cbd5e1;padding-bottom:5px;margin-top:24px;font-size:14pt;font-family:Segoe UI,Calibri,sans-serif;">\1</h2>', text)
    text = re.sub(r'(?m)^#{3}\s+(.+)$', r'<h3 style="color:#2563eb;margin-top:18px;font-size:12pt;font-family:Segoe UI,Calibri,sans-serif;">\1</h3>', text)
    text = re.sub(r'(?m)^#{4}\s+(.+)$', r'<h4 style="color:#334155;margin-top:14px;font-size:11pt;font-family:Segoe UI,Calibri,sans-serif;">\1</h4>', text)
    
    # Horizontal rule
    text = re.sub(r'(?m)^---+$', r'<hr style="border:0;border-top:1.5px solid #cbd5e1;margin:24px 0;"/>', text)
    
    # Bold and italic
    text = re.sub(r'\*\*(.+?)\*\*', r'<strong>\1</strong>', text)
    text = re.sub(r'\*(.+?)\*', r'<em>\1</em>', text)
    
    # Blockquotes
    text = re.sub(r'(?m)^>\s*(.+)$', r'<blockquote style="border-left:4px solid #2563eb;margin:12px 0;padding:8px 16px;background:#f1f5f9;color:#334155;font-style:italic;">\1</blockquote>', text)
    
    # Lists
    text = re.sub(r'(?m)^[-*]\s+(.+)$', r'<li style="margin-bottom:4px;color:#1e293b;">\1</li>', text)
    
    # Paragraphs / Linebreaks
    paragraphs = text.split('\n\n')
    formatted_p = []
    for p in paragraphs:
        p = p.strip()
        if not p:
            continue
        if p.startswith('<h') or p.startswith('<table') or p.startswith('<pre') or p.startswith('<hr') or p.startswith('<div') or p.startswith('<blockquote'):
            formatted_p.append(p)
        elif p.startswith('<li'):
            formatted_p.append(f'<ul style="margin:8px 0;padding-left:24px;">{p}</ul>')
        else:
            p_clean = p.replace('\n', '<br/>')
            formatted_p.append(f'<p style="margin:10px 0;text-align:justify;line-height:1.5;color:#1e293b;">{p_clean}</p>')
            
    return '\n'.join(formatted_p)

html_body = md_to_html(processed_tables)

doc_html = f"""<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>
<head>
<meta charset="utf-8">
<!--[if gte mso 9]>
<xml>
<w:WordDocument>
<w:View>Print</w:View>
<w:Zoom>100</w:Zoom>
<w:DoNotOptimizeForBrowser/>
</w:WordDocument>
</xml>
<![endif]-->
<title>Báo Cáo Nghiên Cứu Thuật Toán Greedy Baseline</title>
<style>
@page {{
    size: A4;
    margin: 20mm 20mm 20mm 20mm;
}}
body {{
    font-family: Calibri, 'Segoe UI', Arial, sans-serif;
    font-size: 11pt;
    line-height: 1.5;
    color: #1e293b;
    background: #ffffff;
}}
h1, h2, h3, h4 {{
    page-break-after: avoid;
}}
table {{
    page-break-inside: avoid;
}}
code, pre {{
    font-family: Consolas, 'Courier New', monospace;
}}
</style>
</head>
<body style="max-width:900px;margin:20px auto;padding:20px;">
{html_body}
</body>
</html>"""

with open(DOC_PATH_1, "w", encoding="utf-8") as f:
    f.write(doc_html)
print(f"Generated: {DOC_PATH_1}")

with open(DOC_PATH_2, "w", encoding="utf-8") as f:
    f.write(doc_html)
print(f"Generated: {DOC_PATH_2}")

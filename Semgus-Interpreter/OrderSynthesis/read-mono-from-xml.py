import xml.etree.ElementTree as ET
import sys
import re
import json

sketch_file = sys.argv[1]
xml_file = sys.argv[2]
out_file = sys.argv[3]

find_flag_comment = re.compile(r'/\*#MONO (.+)_([0-9]+)\*/')
split_key = re.compile(r'^(.*[^ \t])[ \t]arg ([0-9]+)$')

def get_monolines(fname):
    out = dict()
    i = 0
    assert(find_flag_comment.search("abc /*#MONO lang_f2_0*/") is not None)
    for line in open(fname,'r').readlines():
        i+=1
        match = find_flag_comment.search(line)
        if match:
            out[i]=f'{match.group(1)} arg {match.group(2)}'
    return out
        

def extract(mono_lines, tree):
    out = dict()
    root = tree.getroot()
    assert root.tag == 'hole_values'
    for child in root:
        assert child.tag == 'hole_value'
        line = int(child.attrib['line'])
        if not line in mono_lines:
            continue
        key = mono_lines[line]
        
        match = split_key.match(key)
        assert match
        
        syntax = match.group(1)
        index = int(match.group(2))
        
        mono_switch = int(child.attrib['value'])
        
        if syntax not in out:
            out[syntax] = []
        
        u = out[syntax]
        while len(u) <= index:
            u.append('?')
        
        u[index] = "increasing" if mono_switch==0 else "decreasing" if mono_switch==1 else "none"
    return out
    

mono_lines = get_monolines(sketch_file)
extraction = extract(mono_lines, ET.parse(xml_file))

json.dump(extraction,open(out_file,'w'),indent=4)

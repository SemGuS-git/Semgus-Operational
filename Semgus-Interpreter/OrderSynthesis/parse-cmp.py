import sys
import re
import pprint

console = pprint.PrettyPrinter(indent=4)
sketch_file = sys.argv[1]
dest_file = sys.argv[2]


r_start = r'^[ \t]*'
r_ws = r'[ \t]*'
r_empty_to_end = r_ws + r'(?://|(?:(?:/\*.*\*/[ \t]*)*$))'
r_cap_sc = r'(;?)'
r_type = r'(?:int[ \t])|(?:bit[ \t])'
r_varname = r'[a-zA-Z_][a-zA-Z_0-9]*'

r_cap_not_op = r'([^ \t()\r\n!\[\]/\\+\-&|^*=<>]+|\(.*\))'
r_any_op = r'\+|-|\*|==|!=|<|<=|>|>=|&&|\|\||!'

# Anything that looks like a variable identifier or field access
find_var = re.compile(r'([^ \t\r\n!()\[\]/\\+\-&|^*=<>]+)')

find_just_var = re.compile(r'^'+r_varname+r'$')



split_expr = re.compile(r_cap_not_op + r_ws + r'($|(?:'+r_any_op+r'))')

find_empty = re.compile(r'^'+r_empty_to_end)

# Field access on a or b
find_not_var = re.compile(r'[ab]\..*')

# Anything that looks like a declaration
find_declare = re.compile(r_start + r'(' + r_type + r')[ \t]*('+r_varname+r')' + r_ws + r_cap_sc)

# Anything that looks like an assignment (maybe with a declaration)
find_assign = re.compile(r_start + r'(?:' + r_type + r')?[ \t]*('+r_varname+r')[ \t]*=' + r_ws + r'([^;]+);' + r_empty_to_end)

# Anything that looks like an "if"
find_if = re.compile(r_start + r'if[ \t]*\((.*)\)[ \t]*(\{?)' + r_empty_to_end)

find_negate = re.compile('^!\((.*)\)$')

find_return = re.compile(r_start + r'return' + r_ws + ';' + r_ws + r'(\}?)' + r_empty_to_end)

find_compare_signature = re.compile(r_start + r'void compare_([^ \t]+)[ \t]*\(.*\)' + r_ws + r_cap_sc + r_empty_to_end)


find_any_braces = re.compile(r'[\{\}]')
just_open_brace = re.compile(r_start + r'\{' + r_empty_to_end)
just_close_brace = re.compile(r_start + r'\}' + r_empty_to_end)

class OpNode:
    def __init__(self,op,ch):
        self.op = op
        self.pad_op = ' ' + op + ' '
        self.ch = ch
    
    def __str__(self):
        return '('+ self.pad_op.join([str(c) for c in self.ch]) + ')'
    def __repr__(self):
        return str(self)


def parse_function_body(lines_iterator):
    values = dict()
    if_stack = []
    
    decl_depths = dict()
    decl_stack = [[]]
    
    expect_brace_next = False
    is_returning = False
    
    def expr_to_ast(expr):
        parts = split_expr.findall(expr)
        assert parts
        
        # Just the last op should be empty
        assert parts[-1][1]==''
        assert all([op for (blob,op) in parts[:-1]])
        
        if len(parts) == 1:
            return flatten_blob(parts[0][0])
        
        (head,op) = parts[0]
        
        node = OpNode(op,[flatten_blob(head)])
        
        for (blob,op) in parts[1:]:
            node.ch.append(flatten_blob(blob))
            if op not in ['',node.op]:
                node = OpNode(op,[node])
        
        return node
    
    def sub_one_var(match):
        var = match.group()
        return  var if find_not_var.match(var) else str(values[var])
        
    def flatten_blob(blob):
        return values[blob] if blob in values else find_var.sub(sub_one_var,blob)
    
    while (line := next(lines_iterator, None)) is not None:
        if find_empty.match(line):
            continue
        
        if expect_brace_next:
            assert just_open_brace.match(line)
            expect_brace_next = False
            continue
        
        if just_close_brace.match(line):
            if len(if_stack) == 0:
                return values
            else:
                if_stack.pop()
                for key in decl_stack.pop():
                    del decl_depths[key]
                continue
        elif is_returning:
            continue
        
        assert not find_any_braces.search(line)
        
        scope_depth = len(decl_stack)-1
        
        match_declare = find_declare.match(line)
        if match_declare:
            var_name = match_declare.group(2)
            assert var_name not in decl_depths
            
            decl_depths[var_name] = scope_depth
            decl_stack[scope_depth].append(var_name)
            
            has_semicolon = match_declare.group(3) != ''
            if has_semicolon:
                continue
        
        match_assign = find_assign.match(line)
        if match_assign:
            var_name = match_assign.group(1)
            rhs = match_assign.group(2)
            
            node = expr_to_ast(rhs)
            
            if var_name != '_out':
                for k in reversed(range(decl_depths[var_name],scope_depth)):
                    (pred,op) = if_stack[k]
                    if isinstance(node,OpNode) and node.op == op:
                        node.ch.append(pred)
                    else:
                        node = OpNode(op,[pred,node])
            
            values[var_name] = node
            
            continue
        
        assert not match_declare
        
        match_if = find_if.match(line)
        if match_if:
            cond = match_if.group(1)
            has_open_brace = match_if.group(2) != ''
            
            if not has_open_brace:
                expect_brace_next = True
            
            match_neg = find_negate.match(cond)
            
            operator = '&&'
            
            if match_neg:
                cond = match_neg.group(1)
                operator = '||'
            
            if_stack.append(( expr_to_ast(cond), operator))
            decl_stack.append([])
            continue
        
        match_return = find_return.match(line)
        if match_return:
            has_close_brace = match_return.group(1) != ''
            if has_close_brace:
                return values
            else:
                is_returning = True
                continue
        
        raise Exception("Mystery line:", line)
    
    
    raise Exception("Expected closing brace")

def extract_compare_functions(lines_iterator):
    results = dict()
    
    pending = []
    
    expect_brace_next = False
    while (line := next(lines_iterator, None)) is not None:
        if pending:
            assert just_open_brace.match(line)
            expect_brace_next = False
            struct_name = pending.pop()
            print("Start compare_"+struct_name)
            results[struct_name] = parse_function_body(lines_iterator)
            print("Done")
            continue
        
        match_compare_signature = find_compare_signature.match(line)
        if match_compare_signature:
            struct_name = match_compare_signature.group(1)
            has_open_brace = match_compare_signature.group(2) != ''
            
            assert struct_name not in results
            
            if has_open_brace:
                print("Start compare_"+struct_name)
                results[struct_name] = parse_function_body(lines_iterator)
                print("Done")
            else:
                assert not pending
                pending.append(struct_name)
            continue
    return results
                
    

all_lines = open(sketch_file,'r').readlines()

results = extract_compare_functions(iter(all_lines))

with open(dest_file,'w') as out:
    for key in results:
        out.write(f'bit compare_{key} ({key} a, {key} b) {{\n')
        out.write(f'    return {results[key]["_out"]};\n')
        out.write('}\n\n')
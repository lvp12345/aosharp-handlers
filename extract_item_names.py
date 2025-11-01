#!/usr/bin/env python3
"""
Script to extract clean item names from the complex Nadybot item database format
and create a simple text file with one item name per line for LootManager autocomplete.
"""

import re
import sys

def extract_item_names(input_file, output_file):
    """Extract item names from the complex database format."""
    item_names = []
    
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        print(f"Processing {len(lines)} lines from {input_file}...")
        
        for line_num, line in enumerate(lines, 1):
            line = line.strip()
            if not line:
                continue
            
            # Look for lines that start with a number followed by a period (item entries)
            # Format: "   123. Item Name Here"
            match = re.match(r'^\s*\d+\.\s+(.+)$', line)
            if match:
                item_name = match.group(1).strip()
                
                # Remove quotes around item names if present
                if item_name.startswith('"') and '"' in item_name[1:]:
                    end_quote = item_name.index('"', 1)
                    item_name = item_name[1:end_quote]
                
                # Skip empty names
                if item_name and item_name not in item_names:
                    item_names.append(item_name)
                    if len(item_names) % 1000 == 0:
                        print(f"Extracted {len(item_names)} items...")
        
        # Sort alphabetically for better autocomplete experience
        item_names.sort()
        
        # Write to output file
        with open(output_file, 'w', encoding='utf-8') as f:
            for item_name in item_names:
                f.write(item_name + '\n')
        
        print(f"Successfully extracted {len(item_names)} unique item names to {output_file}")
        
        # Show some examples
        print("\nFirst 10 items:")
        for i, name in enumerate(item_names[:10]):
            print(f"  {i+1}. {name}")
        
        if len(item_names) > 10:
            print(f"\nLast 5 items:")
            for i, name in enumerate(item_names[-5:], len(item_names)-4):
                print(f"  {i}. {name}")
                
    except Exception as e:
        print(f"Error processing files: {e}")
        return False
    
    return True

if __name__ == "__main__":
    input_file = "Item_Database_Complete_List.txt"
    output_file = "ItemNames.txt"
    
    print("AO Item Database Name Extractor")
    print("=" * 40)
    
    if extract_item_names(input_file, output_file):
        print(f"\n✓ Success! Clean item names saved to {output_file}")
        print("You can now use this file with LootManager for fast autocomplete.")
    else:
        print("\n✗ Failed to extract item names.")
        sys.exit(1)

#!/usr/bin/env python3
"""
Auno.org Item Database Scraper
Scrapes all items from the Anarchy Online item database at auno.org
"""

import requests
import re
import time
import json
from urllib.parse import urlencode
from bs4 import BeautifulSoup

class AunoScraper:
    def __init__(self):
        self.base_url = "https://auno.org/ao/db.php"
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
        })
        self.items = set()  # Use set to avoid duplicates
        
    def search_items(self, **params):
        """Search for items with given parameters"""
        default_params = {
            'cmd': 'search',
            'patch': '18086200',  # Latest patch
        }
        default_params.update(params)
        
        try:
            response = self.session.get(self.base_url, params=default_params, timeout=10)
            response.raise_for_status()
            return self.parse_search_results(response.text)
        except Exception as e:
            print(f"Error searching with params {params}: {e}")
            return []
    
    def parse_search_results(self, html):
        """Parse search results HTML and extract item names"""
        soup = BeautifulSoup(html, 'html.parser')
        items = []
        
        # Find all links that point to item details
        for link in soup.find_all('a', href=re.compile(r'/ao/db\.php\?id=\d+')):
            item_name = link.get_text(strip=True)
            if item_name and item_name not in ['Name']:  # Skip header
                items.append(item_name)
                self.items.add(item_name)
        
        return items
    
    def scrape_by_type(self):
        """Scrape items by different types"""
        item_types = [
            'item',      # All items
            'weapon',    # Weapons
            'armor',     # Armor
            'implant',   # Implants
            'utility',   # Utility items
            'other',     # Other items
            'nano',      # Nano formulas
        ]
        
        print("Scraping by item type...")
        for item_type in item_types:
            print(f"  Scraping {item_type}...")
            items = self.search_items(type=item_type, ql1=1, ql2=500)
            print(f"    Found {len(items)} items")
            time.sleep(1)  # Be nice to the server
    
    def scrape_by_ql_ranges(self):
        """Scrape items by QL ranges to get more comprehensive results"""
        print("Scraping by QL ranges...")
        ql_ranges = [
            (1, 50), (51, 100), (101, 150), (151, 200), (201, 250),
            (251, 300), (301, 350), (351, 400), (401, 450), (451, 500)
        ]
        
        for ql_min, ql_max in ql_ranges:
            print(f"  Scraping QL {ql_min}-{ql_max}...")
            items = self.search_items(ql1=ql_min, ql2=ql_max)
            print(f"    Found {len(items)} items")
            time.sleep(1)
    
    def scrape_by_profession(self):
        """Scrape items by profession requirements"""
        print("Scraping by profession...")
        professions = [
            'adventurer', 'agent', 'bureaucrat', 'doctor', 'enforcer',
            'engineer', 'fixer', 'keeper', 'martial+artist', 'meta-physicist',
            'nano-technician', 'shade', 'soldier', 'trader'
        ]
        
        for prof in professions:
            print(f"  Scraping {prof}...")
            items = self.search_items(prof=prof)
            print(f"    Found {len(items)} items")
            time.sleep(1)
    
    def scrape_alphabetically(self):
        """Try to scrape items alphabetically by searching for common prefixes"""
        print("Scraping alphabetically...")
        # Common item name prefixes in AO
        prefixes = [
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'advanced', 'basic', 'superior', 'enhanced', 'modified', 'improved',
            'nano', 'bio', 'cyber', 'quantum', 'viral', 'omni', 'clan'
        ]
        
        for prefix in prefixes:
            print(f"  Searching for items starting with '{prefix}'...")
            # This might not work with auno's search, but worth trying
            items = self.search_items(name=prefix)
            print(f"    Found {len(items)} items")
            time.sleep(1)
    
    def save_items(self, filename="ao_items.txt"):
        """Save all collected items to a text file"""
        sorted_items = sorted(list(self.items))
        
        with open(filename, 'w', encoding='utf-8') as f:
            for item in sorted_items:
                f.write(f"{item}\n")
        
        print(f"Saved {len(sorted_items)} unique items to {filename}")
        
        # Also save as JSON for potential future use
        json_filename = filename.replace('.txt', '.json')
        with open(json_filename, 'w', encoding='utf-8') as f:
            json.dump(sorted_items, f, indent=2, ensure_ascii=False)
        
        print(f"Also saved as JSON to {json_filename}")
    
    def run_full_scrape(self):
        """Run a comprehensive scrape using multiple methods"""
        print("Starting comprehensive Auno.org item database scrape...")
        print(f"Target URL: {self.base_url}")
        
        # Try different scraping methods
        self.scrape_by_type()
        self.scrape_by_ql_ranges()
        self.scrape_by_profession()
        
        print(f"\nScraping complete! Found {len(self.items)} unique items total.")
        self.save_items()

if __name__ == "__main__":
    scraper = AunoScraper()
    scraper.run_full_scrape()

import pandas as pd

df = pd.read_csv("d:/workspace/fund_recommendation_trae_new/fund_sample.csv")
name_col = '基金简称'
code_col = '基金代码'

def get_last_letter(name):
    if pd.isna(name):
        return None
    if len(name) > 0 and name[-1].isupper() and name[-1].isalpha():
        return name[-1]
    return None

df['last_letter'] = df[name_col].apply(get_last_letter)
ends_with_letter = df[df['last_letter'].notna()]

print(f"Total funds: {len(df)}")
print(f"Funds ending with letter: {len(ends_with_letter)}")
print()

letter_counts = ends_with_letter['last_letter'].value_counts()
print("=== Letter distribution ===")
for letter, count in letter_counts.items():
    print(f"  {letter}: {count}")

print("\n=== Sample for each letter ===")
for letter in sorted(ends_with_letter['last_letter'].unique()):
    subset = ends_with_letter[ends_with_letter['last_letter'] == letter]
    print(f"\n{letter} ({len(subset)} funds):")
    for _, row in subset.head(5).iterrows():
        print(f"  {row[code_col]}: {row[name_col]}")
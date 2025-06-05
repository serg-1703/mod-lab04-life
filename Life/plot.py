import matplotlib.pyplot as plt
import numpy as np
from matplotlib.ticker import MultipleLocator

density = []
generation = []
with open('data.txt', 'r') as file:
    next(file)
    for line in file:
        parts = line.strip().replace(',', '.').split()
        density.append(float(parts[0]))
        generation.append(int(parts[1]))

plt.style.use('seaborn-v0_8-darkgrid')
fig, ax = plt.subplots(figsize=(12, 7))

fig.patch.set_facecolor('white')
ax.set_facecolor('white')

ax.plot(density, generation, 
        marker='s',
        markersize=8,
        linestyle='--',
        color='#E91E63',
        linewidth=2,
        alpha=0.8,
        label='Generations')

ax.plot(density, generation, color='#673AB7', linewidth=3, label='Trend line')

ax.set_xlabel('Initial Density', fontsize=12, fontweight='bold')
ax.set_ylabel('Generations to Stability', fontsize=12, fontweight='bold')
ax.set_title('Game of Life: Stability Analysis', 
             fontsize=14, fontweight='bold', pad=20)

ax.xaxis.set_major_locator(MultipleLocator(0.1))
ax.xaxis.set_minor_locator(MultipleLocator(0.02))
ax.yaxis.set_major_locator(MultipleLocator(100))
ax.yaxis.set_minor_locator(MultipleLocator(50))

max_gen_idx = np.argmax(generation)
ax.annotate(f'Max: {generation[max_gen_idx]} gens\nat density {density[max_gen_idx]:.2f}',
            xy=(density[max_gen_idx], generation[max_gen_idx]),
            xytext=(0.3, 0.9), textcoords='axes fraction',
            arrowprops=dict(facecolor='black', shrink=0.05),
            bbox=dict(boxstyle="round", fc="w"))

ax.legend(loc='upper right', framealpha=1)
ax.grid(True, which='both', linestyle=':', linewidth=0.7)
ax.fill_between(density, generation, color='#E91E63', alpha=0.1)

plt.tight_layout()
plt.savefig('plot_enhanced.png', dpi=300, bbox_inches='tight', transparent=False)
plt.show()
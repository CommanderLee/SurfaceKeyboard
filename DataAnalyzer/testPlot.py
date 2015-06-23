import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

fig = plt.figure()
# ax = Axes3D(fig)
ax = fig.gca(projection='3d')
# ax = fig.add_subplot(111, projection='3d')
X = [1, 1, 2, 2]
Y = [3, 4, 4, 3]
Z = [1, 2, 1, 1]
# ax.plot_surface(X, Y, Z)
ax.plot_trisurf(X, Y, Z)
plt.show()
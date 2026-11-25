// Generates bot indices against the unchanged soldier GLB attributes.
// Requires meshoptimizer 0.24.0 on NODE_PATH.
const fs = require('node:fs');
const path = require('node:path');
const { MeshoptSimplifier } = require('meshoptimizer');

const assets = path.resolve(__dirname, '../WebGame/assets');
const file = fs.readFileSync(path.join(assets, 'soldier.glb'));
const jsonSize = file.readUInt32LE(12);
const gltf = JSON.parse(file.toString('utf8', 20, 20 + jsonSize));
const dataStart = 28 + jsonSize;

function read(index, Type, width, component) {
    const accessor = gltf.accessors[index];
    const view = gltf.bufferViews[accessor.bufferView];
    if (accessor.componentType !== component || accessor.sparse) {
        throw new Error('Formato de vértice incompatível com a geração de LOD.');
    }
    const stride = (view.byteStride ?? width * Type.BYTES_PER_ELEMENT) / Type.BYTES_PER_ELEMENT;
    const source = new Type(file.buffer, file.byteOffset + dataStart + (view.byteOffset ?? 0) +
        (accessor.byteOffset ?? 0), (accessor.count - 1) * stride + width);
    if (stride === width) return source;
    const packed = new Type(accessor.count * width);
    for (let i = 0; i < accessor.count; i++) {
        packed.set(source.subarray(i * stride, i * stride + width), i * width);
    }
    return packed;
}

async function build() {
    await MeshoptSimplifier.ready;
    const parts = {};
    for (const primitive of gltf.meshes[0].primitives) {
        const positions = read(primitive.attributes.POSITION, Float32Array, 3, 5126);
        const normals = read(primitive.attributes.NORMAL, Float32Array, 3, 5126);
        const indices = read(primitive.indices, Uint16Array, 1, 5123);
        const target = Math.max(12, Math.floor(indices.length * .29 / 3) * 3);
        const [simplified] = MeshoptSimplifier.simplifyWithAttributes(
            Uint32Array.from(indices), positions, 3, normals, 3, [.17, .17, .17],
            null, target, .032);
        const name = gltf.materials[primitive.material].name;
        parts[name] = { count: positions.length / 3, indices: Array.from(simplified) };
        console.log(name, indices.length / 3, '→', simplified.length / 3, 'triângulos');
    }
    fs.writeFileSync(path.join(assets, 'soldier-bot-indices.json'), JSON.stringify(parts) + '\n');
}

build().catch(error => { console.error(error); process.exitCode = 1; });

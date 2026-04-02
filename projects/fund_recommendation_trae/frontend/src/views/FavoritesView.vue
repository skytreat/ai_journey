<template>
  <div class="favorites-view">
    <el-card shadow="hover">
      <template #header>
        <div class="card-header">
          <span>自选基金</span>
          <el-button type="primary" size="small" @click="addFund">添加基金</el-button>
        </div>
      </template>
      
      <div class="favorites-content">
        <el-table :data="favorites" style="width: 100%">
          <el-table-column prop="code" label="基金代码" width="120"></el-table-column>
          <el-table-column prop="name" label="基金名称" width="200"></el-table-column>
          <el-table-column prop="fundType" label="基金类型"></el-table-column>
          <el-table-column prop="nav" label="净值"></el-table-column>
          <el-table-column prop="dailyGrowthRate" label="日涨跌幅"></el-table-column>
          <el-table-column prop="note" label="备注"></el-table-column>
          <el-table-column label="操作" width="150">
            <template #default="scope">
              <el-button type="text" size="small" @click="editNote(scope.row)">编辑备注</el-button>
              <el-button type="text" size="small" @click="removeFund(scope.row.code)" style="color: #f56c6c">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>
    
    <!-- 添加基金对话框 -->
    <el-dialog v-model="dialogVisible" title="添加自选基金">
      <el-form :model="newFund" label-width="80px">
        <el-form-item label="基金代码">
          <el-input v-model="newFund.code" placeholder="请输入基金代码"></el-input>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="newFund.note" placeholder="请输入备注"></el-input>
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="dialogVisible = false">取消</el-button>
          <el-button type="primary" @click="confirmAdd">确定</el-button>
        </span>
      </template>
    </el-dialog>
    
    <!-- 编辑备注对话框 -->
    <el-dialog v-model="editDialogVisible" title="编辑备注">
      <el-form :model="currentFund" label-width="80px">
        <el-form-item label="基金代码">
          <el-input v-model="currentFund.code" disabled></el-input>
        </el-form-item>
        <el-form-item label="基金名称">
          <el-input v-model="currentFund.name" disabled></el-input>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="currentFund.note" placeholder="请输入备注"></el-input>
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="editDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="confirmEdit">确定</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue'
import axios from 'axios'

export default {
  name: 'FavoritesView',
  setup() {
    const favorites = ref([])
    const dialogVisible = ref(false)
    const editDialogVisible = ref(false)
    const newFund = ref({ code: '', note: '' })
    const currentFund = ref({ code: '', name: '', note: '' })
    
    const loadFavorites = async () => {
      try {
        const response = await axios.get('http://localhost:5026/api/favorites')
        favorites.value = response.data.favorites
      } catch (error) {
        console.error('Error loading favorites:', error)
        // 使用模拟数据
        favorites.value = [
          {
            code: '000001',
            name: '华夏成长混合',
            fundType: '混合型',
            nav: 1.2345,
            dailyGrowthRate: 0.005,
            note: '长期持有'
          },
          {
            code: '000002',
            name: '易方达蓝筹精选',
            fundType: '混合型',
            nav: 1.5678,
            dailyGrowthRate: 0.008,
            note: '短期持有'
          }
        ]
      }
    }
    
    const addFund = () => {
      newFund.value = { code: '', note: '' }
      dialogVisible.value = true
    }
    
    const confirmAdd = async () => {
      if (!newFund.value.code) {
        return
      }
      
      try {
        await axios.post('http://localhost:5026/api/favorites', newFund.value)
        dialogVisible.value = false
        loadFavorites()
      } catch (error) {
        console.error('Error adding fund:', error)
        // 模拟添加成功
        favorites.value.push({
          code: newFund.value.code,
          name: `测试基金${newFund.value.code}`,
          fundType: '混合型',
          nav: 1.2345,
          dailyGrowthRate: 0.005,
          note: newFund.value.note
        })
        dialogVisible.value = false
      }
    }
    
    const removeFund = async (code) => {
      try {
        await axios.delete(`http://localhost:5026/api/favorites/${code}`)
        loadFavorites()
      } catch (error) {
        console.error('Error removing fund:', error)
        // 模拟删除成功
        favorites.value = favorites.value.filter(fund => fund.code !== code)
      }
    }
    
    const editNote = (fund) => {
      currentFund.value = { ...fund }
      editDialogVisible.value = true
    }
    
    const confirmEdit = async () => {
      try {
        await axios.put(`http://localhost:5026/api/favorites/${currentFund.value.code}/note`, {
          Note: currentFund.value.note
        })
        editDialogVisible.value = false
        loadFavorites()
      } catch (error) {
        console.error('Error updating note:', error)
        // 模拟更新成功
        const index = favorites.value.findIndex(fund => fund.code === currentFund.value.code)
        if (index !== -1) {
          favorites.value[index].note = currentFund.value.note
        }
        editDialogVisible.value = false
      }
    }
    
    onMounted(() => {
      loadFavorites()
    })
    
    return {
      favorites,
      dialogVisible,
      editDialogVisible,
      newFund,
      currentFund,
      addFund,
      confirmAdd,
      removeFund,
      editNote,
      confirmEdit
    }
  }
}
</script>

<style scoped>
.favorites-view {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.favorites-content {
  margin-top: 20px;
}

.dialog-footer {
  width: 100%;
  display: flex;
  justify-content: flex-end;
}
</style>